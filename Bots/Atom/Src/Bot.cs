using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngryWasp.Cli.Args;
using AngryWasp.Helpers;
using DnsClient;
using Nerva.Bots;
using Nerva.Bots.Plugin;

namespace Atom
{
    public class SeedNodeCache
    {
        public ulong LastUpdate { get; set; } = 0;

        public List<string> SeedNodes { get; set; } = new List<string>();
    }

    public class ApiNodeCache
    {
        public ulong LastUpdate { get; set; } = 0;

        public List<string> ApiNodes { get; set; } = new List<string>();
    }

    public class AtomBotConfig : IBotConfig
    {
        public ulong BotId => 450609948246671360;

        public ulong ServerOwnerId => 407511685134549003;

        public ulong ServerId => 439649936414474256;

        public List<ulong> BotChannelIds => new List<ulong>
		{
			450660331405049876, //Atom
            504717279573835832, //AM-XNV
			509444814404714501, //AM-Bots
		};

        public List<ulong> ServerAdminRoleIds => new List<ulong>
		{
			715494617767739475, //NV-HelloAdmin
		};

        public List<ulong> BotCommanderRoleIds => new List<ulong>
		{
			595498219987927050, //NV-BotCommander
            595495919097741322, //AM-BotCommander
		};
        
        public string CmdPrefix => "!";

        private static SeedNodeCache seedCache = new SeedNodeCache();
        private static ApiNodeCache apiCache = new ApiNodeCache();

        public static List<string> GetSeedNodes()
        {
            ulong now = DateTimeHelper.TimestampNow;
            if (now - seedCache.LastUpdate > 60 * 60 || seedCache.SeedNodes.Count == 0)
            {
                seedCache.SeedNodes.Clear();

                var client = new LookupClient();
                var records = client.Query("seed.nerva.one", QueryType.TXT).Answers;

                foreach (var r in records)
                    seedCache.SeedNodes.Add(((DnsClient.Protocol.TxtRecord)r).Text.First());

                seedCache.LastUpdate = now;
            }
            
            return seedCache.SeedNodes;
        }

        public static List<string> GetApiNodes()
        {
            ulong now = DateTimeHelper.TimestampNow;
            if (now - apiCache.LastUpdate > 60 * 60 || apiCache.ApiNodes.Count == 0)
            {
                apiCache.ApiNodes.Clear();

                var client = new LookupClient();
                var records = client.Query("api.nerva.one", QueryType.TXT).Answers;

                foreach (var r in records)
                    apiCache.ApiNodes.Add(((DnsClient.Protocol.TxtRecord)r).Text.First());

                apiCache.LastUpdate = now;
            }
            
            return apiCache.ApiNodes;
        }
    }

    class AtomBot : IBot
    {
        private AtomBotConfig cfg = new AtomBotConfig();

        public IBotConfig Config => cfg;

        public void Init(Arguments args)
        {
            AtomBotConfig.GetSeedNodes();
            AtomBotConfig.GetApiNodes();
        }

        public Task ClientReady()
        {
            Globals.Client.UserJoined += (u) =>
            {
                Sender.SendPrivateMessage(u, "Reply to this message with `!verify` to unlock the Nerva server. Or not...");
                return Task.CompletedTask;
            };
            return Task.CompletedTask;
        }
    }
}
