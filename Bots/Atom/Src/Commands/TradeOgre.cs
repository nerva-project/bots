using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Nerva.Bots;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;
using Newtonsoft.Json;

namespace Atom.Commands
{
    [Command("tradeogre", "Get market info from TradeOgre")]
    public class TradeOgre : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            RequestData rd = Request.Http("https://tradeogre.com/api/v1/ticker/btc-xnv");
            if (!rd.IsError)
            {
                var json = JsonConvert.DeserializeObject<MarketInfo>(rd.ResultString);

                var em = new EmbedBuilder()
                .WithAuthor("TradeOgre Details", Globals.Client.CurrentUser.GetAvatarUrl())
                .WithDescription("The latest pricing from TradeOgre")
                .WithColor(Color.DarkGreen)
                .WithThumbnailUrl("https://nerva.one/content/images/tradeogre-logo.png");

                em.AddField("Volume", Math.Round(json.Volume, 5) + " BTC", true);
                em.AddField("Buy", Math.Round(json.Ask * 100000000.0d, 0) + " sat", true);
                em.AddField("Sell", Math.Round(json.Bid * 100000000.0d, 0) + " sat", true);
                em.AddField("High", Math.Round(json.High * 100000000.0d, 0) + " sat", true);
                em.AddField("Low", Math.Round(json.Low * 100000000.0d, 0) + " sat", true);

                DiscordResponse.Reply(msg, embed: em.Build());
            }
        }
    }
}