using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots.Classes;
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
            try
            {
                RequestData rd = Request.Http("https://tradeogre.com/api/v1/ticker/xnv-btc");
                if (!rd.IsError)
                {
                    var json = JsonConvert.DeserializeObject<MarketInfo>(rd.ResultString);

                    var em = new EmbedBuilder()
                    .WithAuthor("TradeOgre Details", Globals.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("The latest pricing from TradeOgre")
                    .WithColor(Color.DarkGreen)
                    .WithThumbnailUrl("https://nerva.one/content/images/tradeogre-logo.png");

                    em.AddField("Last Price", Math.Round(json.Price * 100000000.0d, 0) + " sat", true);                    
                    em.AddField("Bid", Math.Round(json.Bid * 100000000.0d, 0) + " sat", true);
                    em.AddField("Ask", Math.Round(json.Ask * 100000000.0d, 0) + " sat", true);
                    em.AddField("Volume", Math.Round(json.Volume, 5) + " BTC", true);
                    em.AddField("High", Math.Round(json.High * 100000000.0d, 0) + " sat", true);
                    em.AddField("Low", Math.Round(json.Low * 100000000.0d, 0) + " sat", true);

                    DiscordResponse.Reply(msg, embed: em.Build());
                }
                else 
                {
                    Logger.WriteError("TradeOgre:Error String: " + rd.ErrorString);
                }
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "TradeOgre:Exception:");
            }
        }
    }
}