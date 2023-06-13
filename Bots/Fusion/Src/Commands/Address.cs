using System;
using Discord.WebSocket;
using Nerva.Bots;
using Nerva.Bots.Classes;
using Nerva.Bots.Plugin;
using Nerva.Bots.Helpers;

namespace Fusion.Commands
{
    [Command("address", "Display your fusion tipjar address")]
    public class Address : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                FusionBotConfig cfg = ((FusionBotConfig)Globals.Bot.Config);

                if (!cfg.UserWalletCache.ContainsKey(msg.Author.Id))
                {
                    AccountHelper.CreateNewAccount(msg);
                }
                else
                {
                    Sender.PrivateReply(msg, $"`{cfg.UserWalletCache[msg.Author.Id].Item2}`");
                }
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Address:Exception:");
            }
        }
    }
}