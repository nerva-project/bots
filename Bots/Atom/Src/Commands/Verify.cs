using Discord;
using Discord.WebSocket;
using Nerva.Bots;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;

#pragma warning disable 4014

namespace Atom.Commands
{
    [Command("verify", "Get verified and access the server.")]
    public class Verify : ICommand
    {
        private const ulong UNVERIFIED_USER_ROLE_ID = 715496358089326703;
        private const ulong VERIFIED_USER_ROLE_ID = 715470723812032602;

        public void Process(SocketUserMessage msg)
        {
            AtomBotConfig cfg = ((AtomBotConfig)Globals.Bot.Config);

            if (msg.Author.Id == cfg.BotId)
                return;
            
            IGuild guild = Globals.Client.GetGuild(cfg.ServerId);
            var unverifiedRole = guild.GetRole(UNVERIFIED_USER_ROLE_ID);
            var verifiedRole = guild.GetRole(VERIFIED_USER_ROLE_ID);

            var u = guild.GetUserAsync(msg.Author.Id).Result;

            SocketGuildUser sgu = u as SocketGuildUser;

            if (sgu == null)
                return;

            sgu.RemoveRoleAsync(unverifiedRole).Wait();
            sgu.AddRoleAsync(verifiedRole).Wait();
            
            Log.Write($"{sgu.Username} has verified");
        }
    }
}
