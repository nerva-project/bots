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
		private DateTime _lastUserCheckTime = DateTime.MinValue;

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

				if(_lastReconnectAttempt.AddMinutes(1) < DateTime.Now && _lastUserCheckTime.AddHours(4) < DateTime.Now)
				{
					// This will initially run when bot starts and every 4 hours after that
					_lastUserCheckTime = DateTime.Now;
					UserActivityCheckProcess();
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

		private async Task UserActivityCheckProcess()
		{
			try
			{
				await Logger.WriteDebug("Running UserActivityCheckProcess. Guild Id: " + Globals.Bot.Config.ServerId + " | Discord User file: " + _discordUserFile);

				// Load Discord users from storage to object in memory				
				if(File.Exists(_discordUserFile))
				{
					// Read json file
					_discordUsers = JsonSerializer.Deserialize<Dictionary<ulong, DiscordUser>>(File.ReadAllText(_discordUserFile));
					await Logger.WriteDebug("Users loaded from file. Count: " + _discordUsers.Count);
				}

				if(_discordUsers.Count == 0)
				{
					// No user file found so get them from Discord
					var guild = Globals.Client.GetGuild(Globals.Bot.Config.ServerId);
					var users = guild.GetUsersAsync();
					
					// Loop through users and load them to user object
					await foreach (var user in users)
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

						await Logger.WriteDebug("Users loaded from Discord. Count: " + _discordUsers.Count);

						//Logger.WriteDebug("User Name: " + socketUser.Username + " | Discriminator: " + socketUser.Discriminator + " | Id: " + socketUser.Id + " | Joined: " + socketUser.JoinedAt.ToString() + " | IsBot: " + socketUser.IsBot + " | Roles: " + stringRoles);
						
						// socketUser.Id = Unique User ID of user
						// socketUser.Username = UserName
						// socketUser.Discriminator = 4 digit number after #
					}

					// Initial user pull from Disord so get last activity for each user
					if(_discordUsers.Count > 0)
					{
						var channels = guild.TextChannels;

						await Logger.WriteDebug("Text channel count: " + channels.Count);
						foreach(var channel in channels)
						{
							await Logger.WriteDebug("Channel Id: " + channel.Id + " | Name: " + channel.Name);
							var messages = channel.GetMessagesAsync(5).Flatten();

							await foreach(var message in messages)
							{
								await Logger.WriteDebug("Message Id: " + message.Id + " | Author Id: " + message.Author.Id + " | Author UserName: " + message.Author.Username);
							}
						}
					}


					// Save users to file
					if(_discordUsers.Count > 0)
					{						
						//TODO: Need to update last activity before saving
						//File.WriteAllText(_discordUserFile, JsonSerializer.Serialize(_discordUsers));
					}
				}
		

				// TODO: Kick users that did not verify within 3 days (They only have Unverified User Role and joined over 3 days ago)

				// TODO: Warn users who did not post in a year. Update warned date in memory and storage

				// TODO: If warned date was more than 2 days ago and they have still not spoken, kick user and reset some date (so we do not attempt the kick them again?)

				await Logger.WriteDebug("Finished running UserActivityCheckProcess.");
			}
			catch (Exception ex)
			{
				await Logger.HandleException(ex, "Exception in UserActivityCheckProcess!");
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