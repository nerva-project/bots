using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots.Classes;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;
using Newtonsoft.Json;

#pragma warning disable 4014

namespace Atom.Commands
{
    [Command("inflation", "Get current inflation information")]
    public class Inflation : ICommand
    {
        private int newXnvPerYear = 157788;
        private int newXnvPerMonth = 13149;
        private int newXnvPerWeek = 3024;
        private int newXnvPerDay = 432;

        public void Process(SocketUserMessage msg)
        {
            try
            {
                RequestData rd = Request.ApiAny(AtomBotConfig.GetApiNodes(), "daemon/get_generated_coins", msg.Channel);
                if (!rd.IsError)
                {
                    double coins = Convert.ToDouble(rd.ResultString);

                    var em = new EmbedBuilder()
                    .WithAuthor("Inflation Info", Globals.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("NERVA is already in tail emission which means that each block has 0.3 XNV (+ tx fee) miner reward. Below numbers are estimates")
                    .WithColor(Color.DarkGrey)
                    .WithThumbnailUrl(Globals.Client.CurrentUser.GetAvatarUrl());

                    em.AddField("Current annual inflation", ((double)newXnvPerYear / coins).ToString("P3"));

                    em.AddField("New XNV per day", newXnvPerDay);
                    em.AddField("New XNV per week", newXnvPerWeek);
                    em.AddField("New XNV per month", newXnvPerMonth);
                    em.AddField("New XNV per year", newXnvPerYear);

                    DiscordResponse.Reply(msg, embed: em.Build());
                }
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Inflation:Exception:");
            }
        }
    }
}