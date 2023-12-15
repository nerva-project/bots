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


                double priceBtcTO = 0;
                double volumeBtcTO = 0;

                double priceUsdtTO = 0;
                double volumeUsdtTO = 0;

                double priceUsdtXE = 0;
                double volumeUsdtXE = 0;

                int ct = 0;

                RequestData rdXnv = Request.Http("https://api.xeggex.com/api/v2/market/getbysymbol/XNV_USDT");            
                if (!rdXnv.IsError)
                {
                    var json = JsonConvert.DeserializeObject<MarketInfoXE>(rdXnv.ResultString);

                    priceUsdtXE = json.LastPrice;
                    volumeUsdtXE = json.VolumeUsdt;
                    ct++;
                }
                else
                {
                    Logger.WriteError("Market:XE XNV_USDT Error String: " + rdXnv.ErrorString);
                }

                rdXnv = Request.Http("https://tradeogre.com/api/v1/ticker/xnv-usdt");            
                if (!rdXnv.IsError)
                {
                    var json = JsonConvert.DeserializeObject<MarketInfo>(rdXnv.ResultString);

                    priceUsdtTO = json.Price;
                    volumeUsdtTO = json.Volume;
                    ct++;
                }
                else
                {
                    Logger.WriteError("Market:TO XNV_USDT Error String: " + rdXnv.ErrorString);
                }

                rdXnv = Request.Http("https://tradeogre.com/api/v1/ticker/xnv-btc");            
                if (!rdXnv.IsError)
                {
                    var json = JsonConvert.DeserializeObject<MarketInfo>(rdXnv.ResultString);

                    priceBtcTO = json.Price;
                    volumeBtcTO = json.Volume;
                    ct++;
                }
                else
                {
                    Logger.WriteError("Market:TO XNV_BTC Error String: " + rdXnv.ErrorString);
                }


                // For now just get average last price of 3 markets. Price is in USDT
                double averagePrice = (priceUsdtXE + priceUsdtTO + (priceBtcTO * btcPrice)) / ct;
                double totalVolume = volumeUsdtXE + volumeUsdtTO + (volumeBtcTO * btcPrice);
        
                if (averagePrice > 0)
                {
                    var em = new EmbedBuilder()
                    .WithAuthor("Market Information", Globals.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("The latest market information")
                    .WithColor(Color.DarkGreen)
                    .WithThumbnailUrl("https://nerva.one/content/images/nerva-logo.png");
                    
                    em.AddField("Market Cap (USDT)", "$" + (circulatingSupply * averagePrice).ToString("N0"), true);
                    em.AddField(" | ", " | ", true);
                    em.AddField("Market Cap (BTC)", (circulatingSupply * (averagePrice / btcPrice)).ToString("N2") + " ₿", true);
                    
                    em.AddField("Last Price (USDT)", "$" + averagePrice.ToString("N4"), true);
                    em.AddField(" | ", " | ", true);
                    em.AddField("Last Price (BTC)", Math.Round(averagePrice / btcPrice, 0) + " sat", true);
                    
                    em.AddField("24h Volume (USDT)", "$" + totalVolume.ToString("N2"), true);
                    em.AddField(" | ", " | ", true);
                    em.AddField("24h Volume (BTC)", Math.Round(totalVolume / btcPrice, 5) + " ₿", true);

                    em.AddField("Circulating Supply", circulatingSupply.ToString("N0") + " XNV", false);

                    DiscordResponse.Reply(msg, embed: em.Build());
                }
                else 
                {
                    Logger.WriteError("Market:XNV Error Average Price is 0 :()");
                }
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Market:Exception:");
            }
        }
    }
}