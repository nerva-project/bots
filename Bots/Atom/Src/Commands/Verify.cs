using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots;
using Nerva.Bots.Plugin;
using Nerva.Bots.Helpers;

#pragma warning disable 4014

namespace Atom.Commands
{
    [Command("verify", "Get verified and access the server.")]
    public class Verify : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                AtomBotConfig cfg = ((AtomBotConfig)Globals.Bot.Config);

                if (msg.Author.Id == cfg.BotId)
                {
                    return;
                }
                
                IGuild guild = Globals.Client.GetGuild(cfg.ServerId);
                var unverifiedRole = guild.GetRole(Constants.UNVERIFIED_USER_ROLE_ID);
                var verifiedRole = guild.GetRole(Constants.VERIFIED_USER_ROLE_ID);

                var u = guild.GetUserAsync(msg.Author.Id).Result;

                SocketGuildUser sgu = u as SocketGuildUser;

                if (sgu == null)
                {
                    return;
                }

                sgu.RemoveRoleAsync(unverifiedRole).Wait();
                sgu.AddRoleAsync(verifiedRole).Wait();
                
                Logger.WriteDebug($"{sgu.Username} has verified");
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Verify:Exception:");
            }
        }
    }
}
