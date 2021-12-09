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
using Log_Severity = AngryWasp.Logger.Log_Severity;
using Log = Nerva.Bots.Helpers.Log;

namespace Nerva.Bots
{
    public class BotRunner
    {
		private string _decryptedToken = null;
		private DiscordSocketClient _client = new DiscordSocketClient();

		private System.Timers.Timer _keepAliveTimer;
		private int _reconnectCount = 0;
		private DateTime _lastReconnectAttempt = DateTime.MinValue;

		private const int _keepAliveInterval = 60000;		// 1 minute
		

        [STAThread]
		public static void Main(string[] args)
		{
			try	
			{
				new BotRunner().MainAsync(args).GetAwaiter().GetResult();
				
			}
			catch (Exception ex)
			{
				Log.Write(Log_Severity.Fatal, ex.Message + "\r\n" + ex.StackTrace);
				Environment.Exit(0);
			}
		}
			
		public async Task MainAsync(string[] rawArgs)
		{
            Arguments args = Arguments.Parse(rawArgs);
            AngryWasp.Logger.Log.CreateInstance(true);

            string token = args.GetString("token", Environment.GetEnvironmentVariable("BOT_TOKEN"));

			if (string.IsNullOrEmpty(token))
			{
				await Log.Write(Log_Severity.Fatal, "Bot token not provided!");
				Environment.Exit(0);
			}
			else
				await Log.Write($"Loaded token {token}");

			string pw = args.GetString("password", Environment.GetEnvironmentVariable("BOT_TOKEN_PASSWORD"));

			if (string.IsNullOrEmpty(pw))
                pw = PasswordPrompt.Get("Please enter your token decryption password");

			try {
				_decryptedToken = token.Decrypt(pw);
			} catch {
				await Log.Write(Log_Severity.Fatal, $"Incorrect password: {pw}");
				Environment.Exit(0);
			}

			string botAssembly = args.GetString("bot");

			if (String.IsNullOrEmpty(botAssembly) ||
				!File.Exists(botAssembly))
			{
				await Log.Write(Log_Severity.Fatal, "Could not load bot plugin!");
				Environment.Exit(0);
			}

			if (args["debug"] != null)
				Globals.RpcLogConfig = Nerva.Rpc.Log.Presets.Normal;

			List<int> errorCodes = new List<int>();

			for (int i = 0; i < args.Count; i++)
			{
				if (args[i].Flag == "debug-hide")
				{
					int j = 0;
					if (int.TryParse(args[i].Value, out j))
						errorCodes.Add(-j);
				}
			}

			if (errorCodes.Count > 0)
				Globals.RpcLogConfig.SuppressRpcCodes = errorCodes;

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
					continue;

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
					Log.Write(Log_Severity.Info, "KeepAlive: Reset reconnect count");
				}

				if(_client == null || _client.ConnectionState == ConnectionState.Disconnected)
				{					
					if(_reconnectCount < 5)
					{
						_reconnectCount++;
						_lastReconnectAttempt = DateTime.Now;

						if(_client == null)
						{
							Log.Write(Log_Severity.Info, "KeepAlive: Creating new DiscordSocketClient. Reconnect count: " + _reconnectCount);
							_client = new DiscordSocketClient();
						}
						
						Log.Write(Log_Severity.Info, "KeepAlive: Trying to connect to Discord. Reconnect count: " + _reconnectCount);

						Globals.Client = _client;
						_client.Log += Log.Write;

						_client.LoginAsync(TokenType.Bot, _decryptedToken);
						_client.StartAsync();

						_client.MessageReceived += MessageReceived;
						_client.Ready += ClientReady;
						_client.Disconnected += (e) =>
						{
							return Task.CompletedTask;
						};

						Log.Write(Log_Severity.Info, "KeepAlive: Connected to Discord");
					}
					else 
					{
						// If reconnect attempt fails 5 times, exit
						Log.Write(Log_Severity.Info, "KeepAlive: Too many reconnect attempts. Quitting...");
						Environment.Exit(1);
					}
				}
            }
            catch (Exception ex)
            {
				Log.Write(Log_Severity.Warning, "KeepAlive: " + ex.Message + "\r\n" + ex.StackTrace);
            }
            finally
            {
				// Restart timer
				if (_keepAliveTimer == null)
				{
					Log.Write(Log_Severity.Warning, "KeepAlive: Timer is NULL. Recreating...");
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
				await Log.WriteNonFatalException(ex);
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