using System;
using System.IO;
using System.Threading.Tasks;
using AngryWasp.Helpers;
using Nerva.Bots.Plugin;
using Nerva.Rpc;
using Nerva.Rpc.Wallet;
using Newtonsoft.Json;
using System.Collections.Generic;
using Fusion.Commands.Gaming;
using AngryWasp.Cli;
using AngryWasp.Cli.Args;
using Nerva.Bots.Helpers;

namespace Fusion
{
    public class FusionBotConfig : IBotConfig
    {
		public ulong BotId => 914358923316850728;

		public ulong ServerId => 439649936414474256;

		public ulong ServerOwnerId => 385624888918016012;

        public List<ulong> BotChannelIds => new List<ulong>
		{
			450660331405049876, // Atom
            466873635638870016, // Fusion
			687440519059472414  // Mee6
		};

		public List<ulong> ServerAdminRoleIds => new List<ulong>
		{
			439651263479545857 	// Admin
		};

		public List<ulong> BotCommanderRoleIds => new List<ulong>
		{
			550591115129257985 // Enforcer
		};

        public string CmdPrefix => "$";

		public string WalletHost { get; } = "127.0.0.1";

        public uint DonationWalletPort { get; set; }

		public uint UserWalletPort { get; set; }

        public AccountJson AccountJson { get; set; } = null;

        public string DonationPaymentIdKey { get; set; } = null;

		public Dictionary<ulong, Tuple<uint, string>> UserWalletCache { get; } = new Dictionary<ulong, Tuple<uint, string>>();

		public string DataDir { get; set; } = Environment.CurrentDirectory;
    }

    public class FusionBot : IBot
    {
        private FusionBotConfig cfg = new FusionBotConfig();
        public IBotConfig Config => cfg;

        public Task ClientReady() => Task.CompletedTask;

        public void Init(Arguments args)
        {
			AngryWasp.Serializer.Serializer.Initialize();

			int randomPort = MathHelper.Random.NextInt(10000, 50000);

			cfg.DonationWalletPort = (uint)args.GetInt("donation-wallet-port", randomPort);
			cfg.UserWalletPort = (uint)args.GetInt("user-wallet-port", randomPort + 1);
			cfg.DataDir = args.GetString("data-dir", Environment.CurrentDirectory);

			string donationWalletFile = args.GetString("donation-wallet-file", null);
			string userWalletFile = args.GetString("user-wallet-file", null);

			if (donationWalletFile == null)
			{
				Logger.WriteError("--donation-wallet-file not specified");
			}

			if (userWalletFile == null)
			{
				Logger.WriteError("--user-wallet-file not specified");
			}
			
			string donationWalletPassword = string.Empty;
			string userWalletPassword = string.Empty;

			if (args["key-file"] != null)
			{
				string[] keys = File.ReadAllLines(args["key-file"].Value);
				string keyFilePassword = args.GetString("key-password", Environment.GetEnvironmentVariable("FUSION_KEY_PASSWORD"));

				if (keyFilePassword == null)	
					keyFilePassword = PasswordPrompt.Get("Please enter the key file decryption password");

				donationWalletPassword = keys[0].Decrypt(keyFilePassword);
				userWalletPassword = keys[1].Decrypt(keyFilePassword);
				cfg.DonationPaymentIdKey = keys[2].Decrypt(keyFilePassword);

				keyFilePassword = null;
			}
			else
			{
				donationWalletPassword = PasswordPrompt.Get("Please enter the donation wallet password");
				userWalletPassword = PasswordPrompt.Get("Please enter the user wallet password");
				cfg.DonationPaymentIdKey = PasswordPrompt.Get("Please enter the payment id encryption key");
			}

			string jsonFile = Path.Combine(cfg.DataDir, $"{donationWalletFile}.json");
			Logger.WriteDebug($"Loading Wallet JSON: {jsonFile}");
			cfg.AccountJson = JsonConvert.DeserializeObject<AccountJson>(File.ReadAllText(jsonFile));

			new OpenWallet(new OpenWalletRequestData 
			{
				FileName = donationWalletFile,
				Password = donationWalletPassword
			},
			(string result) => 
			{
				Logger.WriteDebug("Wallet loaded");
			},
			(RequestError error) => 
			{
				Logger.WriteError("Failed to load donation wallet");
				Environment.Exit(1);
			}, 
			cfg.WalletHost, cfg.DonationWalletPort).Run();

			new OpenWallet(new OpenWalletRequestData {
				FileName = userWalletFile,
				Password = userWalletPassword
			},
			(string r1) =>
			{
				Logger.WriteDebug("Wallet loaded");
				new GetAccounts(null, (GetAccountsResponseData r2) =>
				{
					foreach (var a in r2.Accounts)
					{
						ulong uid = 0;

						if (ulong.TryParse(a.Label, out uid))
						{
							if (!cfg.UserWalletCache.ContainsKey(uid))
							{
								cfg.UserWalletCache.Add(uid, new Tuple<uint, string>(a.Index, a.BaseAddress));
								Logger.WriteDebug($"Loaded wallet for user: {a.Label} - {a.BaseAddress}");
							}
							else
								Logger.WriteDebug($"Duplicate wallet detected for user with id: {uid}");
						}
						else
						{
							//fusion owns address index 0
							if (a.Index == 0)
								cfg.UserWalletCache.Add(cfg.BotId, new Tuple<uint, string>(a.Index, a.BaseAddress));
							else
								Logger.WriteDebug($"Account index {a.Index} is not associated with a user");
						}
					}
				},
				(RequestError error) =>
				{
					Logger.WriteError("Failed to load user wallet");
					Environment.Exit(1);
				},
				cfg.WalletHost, cfg.UserWalletPort).Run();
			},
			(RequestError error) =>
			{
				Logger.WriteError("Failed to load user wallet");
				Environment.Exit(1);
			},
			cfg.WalletHost, cfg.UserWalletPort).Run();

			string fp = Path.Combine(cfg.DataDir, "lottery.xml");

			if (File.Exists(fp))
				LotteryManager.Load(fp);
			else
				LotteryManager.Start();
        }
    }
}