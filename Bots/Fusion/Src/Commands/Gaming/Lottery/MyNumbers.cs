using System;
using Discord.WebSocket;
using Nerva.Bots;
using Nerva.Bots.Plugin;
using Nerva.Bots.Helpers;

namespace Fusion.Commands.Gaming
{
    [Command("mynumbers", "Shows what numbers you have bought in the lottery")]
    public class MyNumbers : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                Sender.PublicReply(msg, LotteryManager.CurrentGame.GetUsersNumbers(msg.Author.Id));
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "MyNumbers:Exception:");
            }
        }
    }
}