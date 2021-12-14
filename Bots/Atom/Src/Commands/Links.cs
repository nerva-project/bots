using System;
using System.Text;
using Discord;
using Discord.WebSocket;
using Nerva.Bots;
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
                Request.Http("https://nerva.one/getbinaries.php", (rd) =>
                {
                    if (!rd.IsError)
                    {
                        var json = JsonConvert.DeserializeObject<LinkData>(rd.ResultString);

                        var em = new EmbedBuilder()
                        .WithAuthor("Download Links", Globals.Client.CurrentUser.GetAvatarUrl())
                        .WithDescription($"Current CLI: {json.CliVersion}\nCurrent GUI: {json.GuiVersion}")
                        .WithColor(Color.DarkPurple)
                        .WithThumbnailUrl("https://nerva.one/content/images/dropbox-logo.png");

                        StringBuilder sb = new StringBuilder();

                        sb.AppendLine($"Windows: [CLI]({json.WindowsLink}) | [GUI]({json.WindowsGuiLink})");
                        sb.AppendLine($"Linux: [CLI]({json.LinuxLink}) | [GUI]({json.LinuxGuiLink})");
                        sb.AppendLine($"MacOS: [CLI]({json.MacLink}) | [GUI]({json.MacGuiLink})");
                        sb.AppendLine($"Ledger: [All Platforms]({json.LedgerLink})");

                        em.AddField($"Nerva Tools", sb.ToString());
                        em.AddField($"Chain Data", $"[QuickSync]({json.QuickSyncLink})");

                        DiscordResponse.Reply(msg, embed: em.Build());
                    }
                });
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "Links:Exception:");
            }
        }
    }
}