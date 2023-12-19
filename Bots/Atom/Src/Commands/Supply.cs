using System;
using Discord.WebSocket;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;

#pragma warning disable 4014

namespace Atom.Commands
{
    [Command("supply", "Get the current circulating supply")]
    public class Supply : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                RequestData rd = Request.ApiAny(AtomBotConfig.GetApiNodes(), "daemon/get_generated_coins", msg.Channel);
                if (!rd.IsError)
                {
                    double coins = Convert.ToDouble(rd.ResultString);
                    DiscordResponse.Reply(msg, text: $"Current Supply: {coins.ToString("N0")}");
                }
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Supply:Exception:");
            }
        }
    }
}