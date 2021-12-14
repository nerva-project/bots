using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots;
using Nerva.Bots.Plugin;
using Nerva.Bots.Helpers;
using System.Linq;

#pragma warning disable 4014

namespace Atom.Commands
{
    [Command("unverify", "Manually set the Unverified user role for all unverified users. Admin use only")]
    public class Unverify : ICommand
    {
        private const ulong UNVERIFIED_USER_ROLE_ID = 715496358089326703;
        private const ulong VERIFIED_USER_ROLE_ID = 715470723812032602;

        public void Process(SocketUserMessage msg)
        {
            try
            {
                AtomBotConfig cfg = ((AtomBotConfig)Globals.Bot.Config);

                //can only be run by the server owner
                if (msg.Author.Id != cfg.ServerOwnerId)
                {
                    Sender.PublicReply(msg, "This command is not for you!");
                    return;
                }

                IGuild guild = Globals.Client.GetGuild(cfg.ServerId);

                var unverifiedRole = guild.GetRole(UNVERIFIED_USER_ROLE_ID);
                var users = guild.GetUsersAsync(CacheMode.AllowDownload).Result;

                int count = 0;
                foreach (var u in users)
                {
                    SocketGuildUser sgu = (SocketGuildUser)u;
                    var hasVerifiedRole = sgu.Roles.Where(x => x.Id == VERIFIED_USER_ROLE_ID).Count() > 0;
                    var hasUnverifiedRole = sgu.Roles.Where(x => x.Id == UNVERIFIED_USER_ROLE_ID).Count() > 0;
                    if (hasVerifiedRole || hasUnverifiedRole)
                    {
                        continue;
                    }

                    if (!hasUnverifiedRole)
                    {
                        sgu.AddRoleAsync(unverifiedRole).Wait();
                    }

                    hasUnverifiedRole = sgu.Roles.Where(x => x.Id == UNVERIFIED_USER_ROLE_ID).Count() > 0;

                    if (!hasUnverifiedRole)
                    {
                        Logger.WriteDebug($"Could not make {sgu.Username} unverified.");
                        continue;
                    }

                    ++count;
                }

                Logger.WriteDebug($"Unverified {count} users");

            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Unverify:Exception:");
            }
        }
    }
}