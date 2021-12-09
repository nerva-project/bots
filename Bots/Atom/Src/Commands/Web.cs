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
            var em = new EmbedBuilder()
            .WithAuthor("Web Links", Globals.Client.CurrentUser.GetAvatarUrl())
            .WithDescription("Need more NERVA information?")
            .WithColor(Color.DarkGrey)
            .WithThumbnailUrl(Globals.Client.CurrentUser.GetAvatarUrl());

            em.AddField("Website", "[nerva.one](https://nerva.one/)");
            em.AddField("Twitter", "[@NervaCurrency](https://twitter.com/NervaCurrency)");
            em.AddField("Reddit", "[r/Nerva](https://www.reddit.com/r/Nerva)");
            em.AddField("Source Code", "[BitBucket](https://bitbucket.org/nerva-project)");
            em.AddField("Block Explorer", "[explorer.nerva.one](https://explorer.nerva.one)");
            em.AddField("CPU Benchmarks", "[Forkmaps.com](https://forkmaps.com/#/benchmarks)");
            em.AddField("Public Node hosted by Hooftly", "[pubnodes.com](https://www.pubnodes.com/) | [Explorer](https://xnvex.pubnodes.com)");

            DiscordResponse.Reply(msg, embed: em.Build());
        }
    }
}