using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots.Classes;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;
using Newtonsoft.Json;

namespace Atom.Commands
{
    [Command("coingecko", "Get info from CoinGecko")]
    public class CoinGecko : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                RequestData rd = Request.Http("https://api.coingecko.com/api/v3/coins/nerva?localization=false");
                if (!rd.IsError)
                {
                    var json = JsonConvert.DeserializeObject<CoinGeckoInfo>(rd.ResultString);

                    var em = new EmbedBuilder()
                    .WithAuthor("CoinGecko Details", Globals.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("The latest scores and rankings from CoinGecko")
                    .WithColor(Color.DarkTeal)
                    .WithThumbnailUrl("https://nerva.one/content/images/coingecko-logo.png");

                    em.AddField("CoinGecko Rank", string.IsNullOrEmpty(json.CoinGeckoRank) ? "Not Provided" : json.CoinGeckoRank, true);
                    em.AddField("CoinGecko Score", string.IsNullOrEmpty(json.CoinGeckoScore) ? "Not Provided" : json.CoinGeckoScore, true);
                    em.AddField("Market Cap Rank", string.IsNullOrEmpty(json.MarketCapRank) ? "Not Provided" : json.MarketCapRank, true);
                    em.AddField("Community Score", string.IsNullOrEmpty(json.CommunityScore) ? "Not Provided" : json.CommunityScore, true);
                    em.AddField("Developer Score", string.IsNullOrEmpty(json.DeveloperScore) ? "Not Provided" : json.DeveloperScore, true);
                    em.AddField("Public Interest Score", string.IsNullOrEmpty(json.PublicInterestScore) ? "Not Provided" : json.PublicInterestScore, true);

                    DiscordResponse.Reply(msg, embed: em.Build());
                }
                else 
                {
                    Logger.WriteError("CoinGecko:Error String: " + rd.ErrorString);
                }
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "CoinGecko:Exception:");
            }
        }
    }
}