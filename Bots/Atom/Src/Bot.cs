using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngryWasp.Helpers;
using DnsClient;
using Nerva.Bots.Plugin;

namespace Atom
{
    public class SeedNodeCache
    {
        public ulong LastUpdate { get; set; } = 0;

        public List<string> SeedNodes { get; set; } = new List<string>();
    }

    public class AtomBotConfig : IBotConfig
    {
        public ulong BotId => 450609948246671360;

        public List<ulong> BotChannelIds => new List<ulong>
		{
			450660331405049876, //Atom
			595232529456562198, //CB-General
			595231506209701908, //CB-ST
            504717279573835832, //AM-XNV
			509444814404714501, //AM-Bots
            510621605479710720, //LB-General
		};

        public List<ulong> DevRoleIds => new List<ulong>
		{
			595498219987927050, //NV-BotCommander
            595495919097741322, //AM-BotCommander
            595495392632766474, //LB-BotCommander
		};
        
        public string CmdPrefix => "!";

        private static SeedNodeCache seedCache = new SeedNodeCache();

        public static List<string> GetSeedNodes()
        {
            ulong now = DateTimeHelper.TimestampNow;
            if (now - seedCache.LastUpdate > 60 * 60 || seedCache.SeedNodes.Count == 0)
            {
                seedCache.SeedNodes.Clear();

                var client = new LookupClient();
                var records = client.Query("seed.getnerva.org", QueryType.TXT).Answers;

                foreach (var r in records)
                    seedCache.SeedNodes.Add(((DnsClient.Protocol.TxtRecord)r).Text.First());

                seedCache.LastUpdate = now;
            }
            
            return seedCache.SeedNodes;
        }
    }

    class AtomBot : IBot
    {
        private AtomBotConfig cfg = new AtomBotConfig();

        public IBotConfig Config => cfg;

        public void Init(CommandLineParser cmd)
        {
            AtomBotConfig.GetSeedNodes();
        }

        public Task ClientReady()
        {
            return Task.CompletedTask;
        }
    }
}
