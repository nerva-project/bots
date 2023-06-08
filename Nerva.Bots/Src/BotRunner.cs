using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngryWasp.Cli;
using AngryWasp.Cli.Args;
using AngryWasp.Helpers;
using Discord;
using Discord.WebSocket;
using Nerva.Bots.Commands;
using Nerva.Bots.Plugin;
using Nerva.Bots.Helpers;
using Nerva.Bots.Classes;
using System.Timers;
using System.Text.Json;

namespace Nerva.Bots
{
    public class BotRunner
    {
		private string _decryptedToken = null;
		private DiscordSocketClient _client = null;

		private Timer _keepAliveTimer;
		private int _reconnectCount = 0;
		private DateTime _lastReconnectAttempt = DateTime.MinValue;

		private IDictionary<ulong, DiscordUser> _discordUsers = new Dictionary<ulong, DiscordUser>();
		private string _discordUserFile = Path.Combine(Environment.CurrentDirectory, "DiscordUsers.json");
		private bool _isUserDictionaryChanged = false;
		private DateTime _userDictionarySavedTime = DateTime.Now;
		private DateTime _lastKickProcessTime = DateTime.Now;

		private const int _keepAliveInterval = 60000;		// 1 minute
        [STAThread]
		public static void Main(string[] args)
		{
			new BotRunner().MainAsync(args).GetAwaiter().GetResult();
		}
			
		public async Task MainAsync(string[] rawArgs)
		{
            Arguments args = Arguments.Parse(rawArgs);
			string logFile = args.GetString("log-file");

			if(string.IsNullOrEmpty(logFile))
			{
				AngryWasp.Logger.Log.CreateInstance(true);
				await Logger.WriteDebug("Logging without file");
			}
			else 
			{
				AngryWasp.Logger.Log.CreateInstance(true, logFile);
				await Logger.WriteDebug("Logging to file: " + logFile);
			}
            
            string token = args.GetString("token", Environment.GetEnvironmentVariable("BOT_TOKEN"));

			if (string.IsNullOrEmpty(token))
			{
				await Logger.WriteError("Bot token not provided!");
				Environment.Exit(0);
			}
			else
			{
				await Logger.WriteDebug($"Loaded token {token}");
			}

			string pw = args.GetString("password", Environment.GetEnvironmentVariable("BOT_TOKEN_PASSWORD"));
			if (string.IsNullOrEmpty(pw))
			{
                pw = PasswordPrompt.Get("Please enter your token decryption password");
			}

			try 
			{
				_decryptedToken = token.Decrypt(pw);
			}
			catch(Exception ex)
			{
				await Logger.HandleException(ex, $"Incorrect password: {pw}");
				Environment.Exit(0);
			}

			string botAssembly = args.GetString("bot");

			if (String.IsNullOrEmpty(botAssembly) || !File.Exists(botAssembly))
			{
				await Logger.WriteError("Could not load bot plugin!");
				Environment.Exit(0);
			}

			if (args["debug"] != null)
			{
				Globals.RpcLogConfig = Nerva.Rpc.Log.Presets.Normal;
			}

			List<int> errorCodes = new List<int>();

			for (int i = 0; i < args.Count; i++)
			{
				if (args[i].Flag == "debug-hide")
				{
					int j = 0;
					if (int.TryParse(args[i].Value, out j))
					{
						errorCodes.Add(-j);
					}
				}
			}

			if (errorCodes.Count > 0)
			{
				Globals.RpcLogConfig.SuppressRpcCodes = errorCodes;
			}

            //load plugin
            Globals.BotAssembly = ReflectionHelper.Instance.LoadAssemblyFile(botAssembly);
			Type botPluginType = ReflectionHelper.Instance.GetTypesInheritingOrImplementing(Globals.BotAssembly, typeof(IBot))[0];
			Globals.Bot = (IBot)Activator.CreateInstance(botPluginType);

			List<Type> botCommands = ReflectionHelper.Instance.GetTypesInheritingOrImplementing(Globals.BotAssembly, typeof(ICommand));

			//common commands for all bots
			botCommands.Add(typeof(Help));
			botCommands.Add(typeof(Ping));

			Globals.Bot.Init(args);

			foreach (Type t in botCommands)
			{
				CommandAttribute ca = t.GetCustomAttribute(typeof(CommandAttribute)) as CommandAttribute;
				if (ca == null)
				{
					continue;
				}

				Globals.BotHelp.Add($"{Globals.Bot.Config.CmdPrefix}{ca.Cmd}", ca.Help);
				Globals.Commands.Add($"{Globals.Bot.Config.CmdPrefix}{ca.Cmd}", t);
			}

			// Timer that will attempt to recover from lost connections
			_keepAliveTimer = new System.Timers.Timer();
			_keepAliveTimer.Interval = 2000;		// Set initial interval to 2 sec
			_keepAliveTimer.Elapsed += (s, e) => KeepAliveProcess();
			_keepAliveTimer.Start();

			await Task.Delay(-1);
        }

		private void KeepAliveProcess()
        {
            try
            {
                if (_keepAliveTimer != null)
                {					
                    _keepAliveTimer.Stop();					
                }

				if(_keepAliveTimer.Interval != _keepAliveInterval)
				{
					// Initial call will be 2 sec so reset it to default
					_keepAliveTimer.Interval = _keepAliveInterval;
				}

				if(_lastReconnectAttempt != DateTime.MinValue && _lastReconnectAttempt.AddHours(1) < DateTime.Now)
				{
					// If last tried to reconnect over an hour ago, reset time/count;
					_reconnectCount = 0;
					_lastReconnectAttempt = DateTime.MinValue;
					Logger.WriteDebug("KeepAlive: Reset reconnect count");
				}

				if(_client == null || _client.ConnectionState == ConnectionState.Disconnected)
				{					
					if(_reconnectCount < 5)
					{
						_reconnectCount++;
						_lastReconnectAttempt = DateTime.Now;

						if(_client != null)
						{
							Logger.WriteDebug("KeepAlive: Disposing of Client...");
							_client.Dispose();
						}

						var config = new DiscordSocketConfig()
						{
							GatewayIntents = GatewayIntents.All
						};

						Logger.WriteDebug("KeepAlive: Creating new Client. Reconnect count: " + _reconnectCount);
						_client = new DiscordSocketClient(config);
						Globals.Client = _client;
						_client.Log += Logger.Write;

						_client.LoginAsync(TokenType.Bot, _decryptedToken);
						_client.StartAsync();

						_client.MessageReceived += MessageReceived;
						_client.Ready += ClientReady;
						_client.Disconnected += (e) =>
						{
							return Task.CompletedTask;
						};

						Logger.WriteDebug("KeepAlive: Connected to Discord");
					}
					else 
					{
						// If reconnect attempt fails 5 times, exit
						Logger.WriteError("KeepAlive: Too many reconnect attempts. Quitting...");
						Environment.Exit(1);
					}
				}

				if(Globals.BotAssembly.GetName().Name.ToLower().Contains("atom"))
				{
					// Only want to run this as Atom
					if(_lastReconnectAttempt.AddMinutes(1) < DateTime.Now && _discordUsers.Count == 0)
					{
						// This should run once shortly after bot starts
						LoadDiscordUsers();
					}

					if(_lastKickProcessTime.AddHours(1) < DateTime.Now)
					{
						// Run once per hour
						KickProcess();
					}

					if(_userDictionarySavedTime.AddMinutes(10) < DateTime.Now && _isUserDictionaryChanged)
					{
						// Save every 10 minutes and only if there were updates
						_userDictionarySavedTime = DateTime.Now;
						_isUserDictionaryChanged = false;
						SaveUserDictionaryToFile();
					}
				}
            }
            catch (Exception ex)
            {
				Logger.HandleException(ex, "KeepAlive: ");
            }
            finally
            {
				// Restart timer
				if (_keepAliveTimer == null)
				{
					Logger.WriteWarning("KeepAlive: Timer is NULL. Recreating...");
					_keepAliveTimer = new System.Timers.Timer();
					_keepAliveTimer.Interval = _keepAliveInterval;
					_keepAliveTimer.Elapsed += (s, e) => KeepAliveProcess();
				}

				_keepAliveTimer.Start();
            }
        }

		private async Task ClientReady()
		{
			await Globals.Bot.ClientReady();
		}

		private async Task MessageReceived(SocketMessage message)
		{
			try	
			{
				var msg = message as SocketUserMessage;
				if (msg == null)
					return;

				if(Globals.BotAssembly.GetName().Name.ToLower().Contains("atom"))
				{
					// Only want to run this as Atom
					UpdateUserActivity(message);
				}

				Regex pattern = new Regex($@"\{Globals.Bot.Config.CmdPrefix}\w+");
				var commands = pattern.Matches(msg.Content.ToLower()).Cast<Match>().Select(match => match.Value).ToArray();

				if (commands.Length == 0)
					return;

#pragma warning disable 4014
				foreach (var c in commands)
					if (Globals.Commands.ContainsKey(c))
					{
						Task.Run(() => {
							((ICommand)Activator.CreateInstance(Globals.Commands[c])).Process(msg);
						});
					}
#pragma warning restore 4014
			}
			catch (Exception ex)
			{
				await Logger.HandleException(ex);
			}
		}

		private void KickProcess()
		{
			try
			{
				foreach(DiscordUser user in _discordUsers.Values)
				{
					bool isUnverified = false;
					bool isVerified = false;
					bool isSafe = false;

					foreach(ulong roleId in user.Roles)
					{
						if(roleId == Constants.EVERYONE_USER_ROLE_ID)
						{
							// Ignore @everyone role
						}
						else if(roleId == Constants.UNVERIFIED_USER_ROLE_ID)
						{							
							isUnverified = true;
						}
						else if(roleId == Constants.ADMIN_USER_ROLE_ID || roleId == Constants.ENFORCER_USER_ROLE_ID)
						{							
							isSafe = true;
							break;
						}
						else if(roleId == Constants.VERIFIED_USER_ROLE_ID)
						{
							isVerified = true;
						}
						else
						{
							// Do not care about other roles so just ignore
						}
					}

					if(isSafe)
					{
						// Those are safe
						continue;
					}
					else if(isVerified)
					{
						if(user.LastPostDate.AddDays(365) < DateTime.Now)
						{
							if(user.WarnedDate == DateTime.MinValue)
							{
								// User has not been warned yet so warn them
								Logger.WriteDebug("Warning inactive user: " + user.UserName + " | Id: " + user.Id + " | Posted: " + user.LastPostDate.ToString());
								//user.WarnedDate = DateTime.Now;
								//_isUserDictionaryChanged = true;


								//TODO: Implement sending warning message to user
							}
							else if(user.WarnedDate.AddDays(1) < DateTime.Now)
							{
								// User has been warned more than 3 days ago and they have not posted so kick them
								Logger.WriteDebug("Kicking inactive user: " + user.UserName + " | Id: " + user.Id + " | Posted: " + user.LastPostDate.ToString());
								//user.KickReason = "Inactive";
								//user.KickDate = DateTime.Now;
								//_isUserDictionaryChanged = true;


								// TODO: Implement kicking user
							}
						}
						else 
						{
							// Reset values if user spoke/rejoined
							if(user.WarnedDate != DateTime.MinValue)
							{
								user.WarnedDate = DateTime.MinValue;
							}

							if(user.KickDate != DateTime.MinValue)
							{
								user.KickDate = DateTime.MinValue;
							}
						}
					}
					else if(isUnverified)
					{
						// Kick unverified user if not verified within 3 days
						if(user.JoinedDate.AddDays(3) < DateTime.Now)
						{
							Logger.WriteDebug("Kicking unverified user: " + user.UserName + " | Id: " + user.Id + " | Joined: " + user.JoinedDate.ToString());
							//user.KickReason = "Unverified";
							//user.KickDate = DateTime.Now;
							//_isUserDictionaryChanged = true;


							// TODO: Implement kicking user
						}
					}
					else 
					{
						// Why are you here?
						Logger.WriteDebug("KickProcess else, User: " + user.UserName + " | Id: " + user.Id);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.HandleException(ex, "KickProcess: ");
			}
		}

		private void UpdateUserActivity(SocketMessage message)
		{
			if(_discordUsers != null && _discordUsers.Count > 0 && message.Author != null && !message.Author.IsBot)
			{
				if(!_discordUsers.ContainsKey(message.Author.Id))
				{
					SocketGuildUser socketUser = (SocketGuildUser)message.Author;
					IEnumerable<SocketRole> userRoles = socketUser.Roles;

					// Add new user to dictionary
					DiscordUser newUser = new DiscordUser();
					newUser.Id = message.Author.Id;
					newUser.UserName = message.Author.Username;
					newUser.Discriminator = message.Author.Discriminator;
					newUser.JoinedDate = message.Author.CreatedAt.DateTime;

					newUser.Roles = new List<ulong>();
					foreach(SocketRole role in userRoles)
					{
						newUser.Roles.Add(role.Id);
					}

					_discordUsers.Add(message.Author.Id, newUser);
				}

				if(_discordUsers.ContainsKey(message.Author.Id) && _discordUsers[message.Author.Id].LastPostDate < message.CreatedAt.DateTime)
				{
					Logger.WriteDebug("Updating last post date for User: " + message.Author.Username + " to: " + message.CreatedAt.DateTime.ToString());
					_discordUsers[message.Author.Id].LastPostDate = message.CreatedAt.DateTime;

					// This way we know that we need to save to file but don't want to do it after every message
					_isUserDictionaryChanged = true;
				}
			}
		}

		private void LoadDiscordUsers()
		{
			try
			{
				Logger.WriteDebug("Running UserActivityCheckProcess. Guild Id: " + Globals.Bot.Config.ServerId + " | Discord User file: " + _discordUserFile);

				if(File.Exists(_discordUserFile))
				{
					// Load Discord users from storage to object in memory	
					_discordUsers = JsonSerializer.Deserialize<Dictionary<ulong, DiscordUser>>(File.ReadAllText(_discordUserFile));
					Logger.WriteDebug("Users loaded from file. Count: " + _discordUsers.Count);

					if(_discordUsers.Count > 0)
					{
						// Refresh users as we don't know why or for how long bot has been down
						GetUserActivityFromDiscord(100);
					}
				}
				else
				{
					// No user file found so get users from Discord
					IGuild guild = Globals.Client.GetGuild(Globals.Bot.Config.ServerId);
					var users = guild.GetUsersAsync(CacheMode.AllowDownload).Result;
					
					// Loop through users and load them to user object
					foreach (var user in users)
					{
						SocketGuildUser socketUser = (SocketGuildUser)user;
						IEnumerable<SocketRole> userRoles = socketUser.Roles;

						if(!socketUser.IsBot)
						{
							if(!_discordUsers.ContainsKey(socketUser.Id))
							{
								DiscordUser discordUser = new DiscordUser();
								discordUser.Id = socketUser.Id;
								discordUser.UserName = socketUser.Username;
								discordUser.Discriminator = socketUser.Discriminator;
								discordUser.JoinedDate = socketUser.JoinedAt.Value.DateTime;

								//string stringRoles = string.Empty;
								discordUser.Roles = new List<ulong>();						
								foreach(SocketRole role in userRoles)
								{
									discordUser.Roles.Add(role.Id);
									//stringRoles += " Id: " + role.Id + ", Name: " + role.Name;
								}

								_discordUsers.Add(socketUser.Id, discordUser);
							}
						}
					
						//Logger.WriteDebug("User Name: " + socketUser.Username + " | Discriminator: " + socketUser.Discriminator + " | Id: " + socketUser.Id + " | Joined: " + socketUser.JoinedAt.ToString() + " | IsBot: " + socketUser.IsBot + " | Roles: " + stringRoles);					
					}

					Logger.WriteDebug("Users loaded from Discord. Count: " + _discordUsers.Count);

					// Initial user pull from Disord so get many messages
					if(_discordUsers.Count > 0)
					{
						GetUserActivityFromDiscord(5000);
					}
				}

				Logger.WriteDebug("Finished running UserActivityCheckProcess.");
			}
			catch (Exception ex)
			{
				Logger.HandleException(ex, "UserActivityCheckProcess: ");
			}
		}

		private async Task GetUserActivityFromDiscord(int numberOfMessages)
		{
			try
			{
				await Logger.WriteDebug("Started running GetUserActivityFromDiscord");

				var socketGuild = Globals.Client.GetGuild(Globals.Bot.Config.ServerId);
				var channels = socketGuild.TextChannels;

				await Logger.WriteDebug("Text channel count: " + channels.Count);
				foreach(SocketTextChannel channel in channels)
				{
					await Logger.WriteDebug("Channel Id: " + channel.Id + " | Name: " + channel.Name);

					if(channel.Category != null && (channel.Category.Name.ToLower().Equals("stats") || channel.Category.Name.ToLower().Equals("archived")))
					{
						// Skip Stats and Archived channels
					}
					else 
					{
						var messages = channel.GetMessagesAsync(numberOfMessages).Flatten();

						await foreach(var message in messages)
						{
							//await Logger.WriteDebug("Message Id: " + message.Id + " | Author Id: " + message.Author.Id + " | Author UserName: " + message.Author.Username);
							if(_discordUsers.ContainsKey(message.Author.Id))
							{
								if(message.CreatedAt.DateTime > _discordUsers[message.Author.Id].LastPostDate)
								{
									await Logger.WriteDebug("Updating Last Post Date for User Id: " + message.Author.Id + " | UserName: " + message.Author.Username + " | Posted: " + message.CreatedAt.DateTime.ToString());
									_discordUsers[message.Author.Id].LastPostDate = message.CreatedAt.DateTime;
								}
							}
							//else 
							//{
							//	await Logger.WriteError("User not found in Dictionary. User Id: " + message.Author.Id + " | UserName: " + message.Author.Username);
							//}
						}
					}

					// Don't go too fast when getting messages from channels
					await Task.Delay(1000);
				}

				// Save users to file
				SaveUserDictionaryToFile();

				await Logger.WriteDebug("Finished running GetUserActivityFromDiscord");
			}
			catch (Exception ex)
			{
				await Logger.HandleException(ex, "GetUserActivityFromDiscord: ");
			}
		}

		private void SaveUserDictionaryToFile()
		{
			try
			{
				if(_discordUsers != null && _discordUsers.Count > 0)
				{						
					File.WriteAllText(_discordUserFile, JsonSerializer.Serialize(_discordUsers));
					Logger.WriteDebug("User Dictionary saved to file. Record count: " + _discordUsers.Count);
				}
			}
			catch (Exception ex)
			{
				Logger.HandleException(ex, "SaveUserDictionaryToFile: ");
			}
		}
    }


    public static class Globals
    {
        public static Assembly BotAssembly { get; set; } = null;

		public static IBot Bot { get; set; } = null;

        public static DiscordSocketClient Client { get; set; }

		public static Nerva.Rpc.Log RpcLogConfig { get; set; } = Nerva.Rpc.Log.Presets.None;

		public static Dictionary<string, string> BotHelp { get; set; } = new Dictionary<string, string>();

		public static Dictionary<string, Type> Commands = new Dictionary<string, Type>();
    }
}