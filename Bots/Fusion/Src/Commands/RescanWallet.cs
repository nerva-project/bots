using System;
using Discord.WebSocket;
using Nerva.Bots;
using Nerva.Bots.Classes;
using Nerva.Bots.Plugin;
using Nerva.Rpc.Wallet;
using Nerva.Bots.Helpers;

#pragma warning disable 4014

namespace Fusion.Commands
{
    [Command("rescan", "Rescans the wallet to fix any funkiness. Not for you")]
    public class RescanWallet : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                FusionBotConfig cfg = ((FusionBotConfig)Globals.Bot.Config);

                //can only be run by the server owner
                if (msg.Author.Id != cfg.ServerOwnerId)
                {
                    Sender.PublicReply(msg, "This command is not for you!");
                    return;
                }

                new RescanBlockchain( (c) =>
                {
                    Sender.PrivateReply(msg, "Rescan of donation wallet complete.");
                }, (e) =>
                {
                    Sender.PrivateReply(msg, "Oof. Couldn't rescan donation wallet.");
                }, cfg.WalletHost, cfg.DonationWalletPort).RunAsync();

                new RescanBlockchain( (c) =>
                {
                    Sender.PrivateReply(msg, "Rescan of user wallet complete.");
                }, (e) =>
                {
                    Sender.PrivateReply(msg, "Oof. Couldn't rescan user wallet.");
                }, cfg.WalletHost, cfg.UserWalletPort).Run();
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "RescanWallet:Exception:");
            }
        }
    }
}