using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;

#pragma warning disable 4014

namespace Atom.Commands
{
    [Command("web", "Get some useful web links")]
    public class Web : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                var em = new EmbedBuilder()
                .WithAuthor("Web Links", Globals.Client.CurrentUser.GetAvatarUrl())
                .WithDescription("Need more NERVA information?")
                .WithColor(Color.DarkGrey)
                .WithThumbnailUrl(Globals.Client.CurrentUser.GetAvatarUrl());

                em.AddField("Website", "[nerva.one](https://nerva.one/)");
                em.AddField("Twitter", "[@NervaCurrency](https://twitter.com/NervaCurrency)");
                em.AddField("Youtube", "[@nervapro](https://www.youtube.com/@nervapro)");
                em.AddField("Source Code", "[GitHub](https://github.com/nerva-project)");
                em.AddField("Block Explorer", "[explorer.nerva.one](https://explorer.nerva.one)");
                em.AddField("Node Map", "[map.nerva.one](https://map.nerva.one)");
                em.AddField("Documentation", "[docs.nerva.one]( https://docs.nerva.one)");

                DiscordResponse.Reply(msg, embed: em.Build());
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Web:Exception:");
            }
        }
    }
}