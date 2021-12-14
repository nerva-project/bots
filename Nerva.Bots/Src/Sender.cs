using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots.Helpers;

namespace Nerva.Bots
{
    public static class Sender
    {
        public static bool IsPrivateMessage(SocketMessage msg) => (msg.Channel.GetType() == typeof(SocketDMChannel));

        public static void PublicReply(SocketUserMessage userMsg, string text, Embed embed = null) => Reply(userMsg, true, text, embed);

        public static void PrivateReply(SocketUserMessage userMsg, string text, Embed embed = null) => Reply(userMsg, false, text, embed);

        public static void SendPrivateMessage(SocketUser user, string text, Embed embed = null)
        {
            try
            {
                Discord.UserExtensions.SendMessageAsync(user, text, false, embed);
            }
            catch (Exception)
            {
                Logger.WriteWarning($"Sending message to {user.Username} failed");
            }
        }

        public static void Reply(SocketUserMessage userMsg, bool allowPublic, string text, Embed embed = null)
        {
            try
            {
                if (text == null)
                    text = string.Empty;

                if (allowPublic)
                    userMsg.Channel.SendMessageAsync(text, false, embed);
                else
                {
                    Discord.UserExtensions.SendMessageAsync(userMsg.Author, text, false, embed);
                    if (!IsPrivateMessage(userMsg))
                        userMsg.DeleteAsync();
                }
            }
            catch (Exception)
            {
                Logger.WriteWarning($"Sending message to {userMsg.Author.Username} failed");
            }
        }
    }
}