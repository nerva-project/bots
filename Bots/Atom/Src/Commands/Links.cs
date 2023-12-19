using System;
using System.Text;
using Discord;
using Discord.WebSocket;
using Nerva.Bots.Classes;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;
using Newtonsoft.Json;

namespace Atom.Commands
{
    [Command("links", "Get official Nerva download links")]
    public class Links : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                    var em = new EmbedBuilder()
                    .WithAuthor("Download Links", Globals.Client.CurrentUser.GetAvatarUrl())
                    .WithDescription($"Current CLI: v0.1.8.0: Speed\nCurrent GUI: v0.3.3.0")
                    .WithColor(Color.DarkPurple)
                    .WithThumbnailUrl("https://nerva.one/content/images/dropbox-logo.png");

                    StringBuilder sb = new StringBuilder();

                    sb.AppendLine($"Windows: [CLI](https://github.com/nerva-project/nerva/releases/download/v0.1.8.0/nerva-v0.1.8.0_windows_minimal.zip) | [GUI](https://github.com/nerva-project/nerva-gui/releases/download/v0.3.3.0/nerva-gui-v0.3.3.0_win-x64.zip)");
                    sb.AppendLine($"Linux: [CLI](https://github.com/nerva-project/nerva/releases/download/v0.1.8.0/nerva-v0.1.8.0_linux_minimal.zip}) | [GUI](https://github.com/nerva-project/nerva-gui/releases/download/v0.3.3.0/nerva-gui-v0.3.3.0_linux-x64.zip)");
                    sb.AppendLine($"MacOS: [CLI](https://github.com/nerva-project/nerva/releases/download/v0.1.8.0/nerva-v0.1.8.0_osx_minimal.zip) | [GUI](https://github.com/nerva-project/nerva-gui/releases/download/v0.3.3.0/nerva-gui-v0.3.3.0_osx-x64.zip)");

                    em.AddField($"Nerva Tools", sb.ToString());
                    em.AddField($"Chain Data", $"[QuickSync](https://nerva.one/quicksync/quicksync.raw)");

                    DiscordResponse.Reply(msg, embed: em.Build());
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Links:Exception:");
            }
        }
    }
}