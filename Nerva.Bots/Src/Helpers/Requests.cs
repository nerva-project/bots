using System;
using System.Collections.Generic;
using Nerva.Bots.Classes;
using AngryWasp.Helpers;
using Discord;
using Discord.WebSocket;

namespace Nerva.Bots.Helpers
{
    public class RequestData
    {
        public string ResultString { get; set; }

        public string ErrorString { get; set; }
        public bool IsError => !string.IsNullOrEmpty(ErrorString);
    }

    public class Request
    {
        public static RequestData ApiAny(List<string> apiLinks, string method, ISocketMessageChannel channel)
        {
            try
            {
                foreach (var apiLink in apiLinks)
                {
                    RequestData rd = Http($"{apiLink}/{method}/");

                    if (!rd.IsError)
                    {
                        return rd;
                    }
                    else 
                    {
                        Logger.WriteWarning("ApiAny:Error String: " + rd.ErrorString);
                    }
                }
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "ApiAny:Exception:");
            }

            channel.SendMessageAsync("Sorry... All API's are down. The zombie apocalyse is upon us! :scream:");
            return null;
        }

        public static void ApiAll(List<string> apiLinks, string method, ISocketMessageChannel channel, Action<Dictionary<string, RequestData>> action)
        {
            try
            {
                Dictionary<string, RequestData> ret = new Dictionary<string, RequestData>();
                foreach (var apiLink in apiLinks)
                {
                    if (ret.ContainsKey(apiLink))
                    {
                        continue;
                    }

                    RequestData rd = Http($"{apiLink}/{method}/");
                    if(rd.IsError)
                    {
                        Logger.WriteError("ApiAll:Error String: " + rd.ErrorString);
                    }

                    ret.Add(apiLink, rd.IsError ? null : rd);                    
                }

                bool allNull = true;

                foreach (var r in ret.Values)
                {
                    if (r != null)
                    {
                        allNull = false;
                        break;
                    }
                }

                if (allNull)
                {
                    channel.SendMessageAsync("Sorry... All API's are down. The zombie apocalyse is upon us! :scream:");
                }
                else
                {
                    action(ret);
                }
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "ApiAll:Exception:");
            }
        }

        public static RequestData Http(string url)
        {
            string returnStr = string.Empty;
            string errorStr = string.Empty;

            if (!NetHelper.HttpRequest(url, out returnStr, out errorStr))
            {
                Logger.WriteError(errorStr);
            }

            return new RequestData
            {
                ErrorString = errorStr,
                ResultString = returnStr
            };
        }

        public static void Http(string url, Action<RequestData> action)
        {
            string returnStr = string.Empty;
            string errorStr = string.Empty;

            if (!NetHelper.HttpRequest(url, out returnStr, out errorStr))
            {
                Logger.WriteError(errorStr);
            }

            action(new RequestData
            {
                ErrorString = errorStr,
                ResultString = returnStr
            });
        }
    }

    public class DiscordResponse
    {
        public static void Reply(SocketUserMessage msg, bool privateOnly = false, string text = null, Embed embed = null)
        {
            try
            {
                if (text == null)
                {
                    text = string.Empty;
                }

                if (msg.Channel.GetType() != typeof(SocketDMChannel))
                {
                    bool isBotCommander = false;
                    var userRoles = ((SocketGuildUser)msg.Author).Roles;
                    foreach(SocketRole role in userRoles)
                    {
                        if (Globals.Bot.Config.BotCommanderRoleIds.Contains(role.Id))
                        {
                            isBotCommander = true;
                            break;
                        }
                    }
                    
                    if (isBotCommander)
                    {
                        msg.Channel.SendMessageAsync(text, false, embed);
                        return;
                    }

                    if (Globals.Bot.Config.BotChannelIds.Contains(msg.Channel.Id) && !privateOnly)
                        msg.Channel.SendMessageAsync(text, false, embed);
                    else
                    {
                        Discord.UserExtensions.SendMessageAsync(msg.Author, text, false, embed);
                        msg.DeleteAsync();
                    }
                }
                else
                {
                    Discord.UserExtensions.SendMessageAsync(msg.Author, text, false, embed);
                }
            }
            catch (Exception ex)
            {
                Logger.HandleException(ex, $"Count not send reply to {msg.Author.Username}");
            }
        }
    }
}