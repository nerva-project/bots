using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots;
using Nerva.Bots.Classes;
using Nerva.Bots.Plugin;
using Nerva.Rpc;
using Nerva.Rpc.Wallet;
using Nerva.Bots.Helpers;

namespace Fusion.Commands
{
    [Command("botjar", "Get information about fusion's wallet")]
    public class BotJar : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                FusionBotConfig cfg = ((FusionBotConfig)Globals.Bot.Config);

                new GetBalance(new GetBalanceRequestData
                {
                    AccountIndex = 0
                },
                (GetBalanceResponseData result) =>
                {
                    EmbedBuilder eb = new EmbedBuilder()
                    .WithAuthor($"Fusion's Tip Jar", Globals.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription("Whale or fail?")
                    .WithColor(Color.DarkTeal)
                    .WithThumbnailUrl(Globals.Client.CurrentUser.GetAvatarUrl());

                    eb.AddField("Address", cfg.UserWalletCache[cfg.BotId].Item2);
                    eb.AddField("Unlocked", $"{result.UnlockedBalance.FromAtomicUnits()} xnv");
                    eb.AddField("Total", $"{result.Balance.FromAtomicUnits()} xnv");

                    Sender.PublicReply(msg, null, eb.Build());
                },
                (RequestError e) =>
                {
                    Sender.PrivateReply(msg, "Oof. No good. You are going to have to try again later.");
                },
                cfg.WalletHost, cfg.UserWalletPort).Run();
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "BotJar:Exception:");
            }
        }
    }
}