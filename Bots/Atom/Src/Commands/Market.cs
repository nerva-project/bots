using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots.Classes;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;
using Newtonsoft.Json;

namespace Atom.Commands
{
    [Command("market", "Get market info such as market cap, supply, price")]
    public class Market : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                double circulatingSupply = 0.0;
                double btcPrice = 0.0;

                RequestData rdSupply = Request.ApiAny(AtomBotConfig.GetApiNodes(), "daemon/get_generated_coins", msg.Channel);
                if (!rdSupply.IsError)
                {
                    circulatingSupply = Convert.ToDouble(rdSupply.ResultString);
                }
                else 
                {
                    Logger.WriteError("Market:Supply Error String: " + rdSupply.ErrorString);
                }

                RequestData rdBtc = Request.Http("https://tradeogre.com/api/v1/ticker/btc-usdt");
                if (!rdBtc.IsError)
                {
                    var json = JsonConvert.DeserializeObject<MarketInfo>(rdBtc.ResultString);
                    btcPrice = Convert.ToDouble(json.Price);
                }
                else 
                {
                    Logger.WriteError("Market:BTC Error String: " + rdBtc.ErrorString);
                }

                RequestData rdXnv = Request.Http("https://tradeogre.com/api/v1/ticker/xnv-btc");            
                if (!rdXnv.IsError)
                {
                    var json = JsonConvert.DeserializeObject<MarketInfo>(rdXnv.ResultString);

                    var em = new EmbedBuilder()
                    .WithAuthor("Market Information", Globals.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("The latest market information")
                    .WithColor(Color.DarkGreen)
                    .WithThumbnailUrl("https://nerva.one/content/images/nerva-logo.png");
                    
                    em.AddField("Market Cap (USD)", "$" + (circulatingSupply * json.Price * btcPrice).ToString("N0"), true);
                    em.AddField("     -     ", "          ", true);
                    em.AddField("Market Cap (BTC)", (circulatingSupply * json.Price).ToString("N2") + "฿", true);
                    
                    em.AddField("Last Price (USD)", "$" + (json.Price * btcPrice).ToString("N4"), true);
                    em.AddField("     -     ", "          ", true);
                    em.AddField("Last Price (BTC)", Math.Round(json.Price * 100000000.0d, 0) + " sat", true);
                    
                    em.AddField("Volume (USD)", "$" + (json.Volume * btcPrice).ToString("N2"), true);
                    em.AddField("     -     ", "          ", true);
                    em.AddField("Volume (BTC)", Math.Round(json.Volume, 5) + " BTC", true);

                    em.AddField("Circulating Supply", circulatingSupply.ToString("N0") + " XNV", false);

                    DiscordResponse.Reply(msg, embed: em.Build());
                }
                else 
                {
                    Logger.WriteError("Market:XNV Error String: " + rdXnv.ErrorString);
                }
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Market:Exception:");
            }
        }
    }
}