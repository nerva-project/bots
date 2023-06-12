using System;
using Discord.WebSocket;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;

namespace Nerva.Bots.Commands
{
    [Command("ping", "Make sure the bot is alive")]
    public class Ping : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                DiscordResponse.Reply(msg, text: "Pong took " + Globals.Client.Latency + " ms");
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Ping:Exception:");
            }
        }
    }
}