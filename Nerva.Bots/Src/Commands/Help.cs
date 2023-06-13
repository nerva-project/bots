using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots.Classes;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;

namespace Nerva.Bots.Commands
{
    [Command("help", "Shows what this little bot can do")]
    public class Help : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                var em = new EmbedBuilder()
                .WithAuthor("Help", Globals.Client.CurrentUser.GetAvatarUrl())
                .WithDescription("How can I help you today?")
                .WithColor(Color.DarkBlue)
                .WithThumbnailUrl(Globals.Client.CurrentUser.GetAvatarUrl());

                foreach (var h in Globals.BotHelp)
                    em.AddField(h.Key, h.Value);

                DiscordResponse.Reply(msg, embed: em.Build());
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Help:Exception:");
            }
        }
    }
}