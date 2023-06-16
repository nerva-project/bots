using System;
using Discord;
using Discord.WebSocket;
using Nerva.Bots.Classes;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;

namespace Atom.Commands
{
    [Command("DiscordVerify", "Used for Auto verification")]
    public class DiscordVerify : ICommand
    {
        public void Process(SocketUserMessage msg)
        {
            try
            {
                if(Globals.DiscordUsers != null && Globals.DiscordUsers.Count > 0 && msg.Author != null && !msg.Author.IsBot)
				{
                    if(!Globals.DiscordUsers.ContainsKey(msg.Author.Id))
                    {
                        Globals.AddUserToDictionary(msg.Author);						
                        Logger.WriteDebug("DiscordVerify added new user to dictionary: " + msg.Author.Username);
                    }

					if(Globals.DiscordUsers.ContainsKey(msg.Author.Id))
                    {
                        bool isUserVerified = false;
                        bool isUserUnverified = false;

                        // First make sure that user not yet verified
                        foreach(ulong roleId in Globals.DiscordUsers[msg.Author.Id].Roles)
                        {
                            if(roleId == Constants.UNVERIFIED_USER_ROLE_ID)
                            {							
                                isUserUnverified = true;
                            }
                            else if(roleId == Constants.VERIFIED_USER_ROLE_ID)
                            {
                                isUserVerified = true;
                            }
                        }

                        if(!isUserVerified && isUserUnverified)
                        {
                            Logger.WriteDebug("DiscordVerify says that user is Unverified only: " + msg.Author.Username);

                            // Only run this if user has Unverified Role and does not have Verified Role
                            IGuild guild = Globals.Client.GetGuild(Globals.Bot.Config.ServerId);
                            var unverifiedRole = guild.GetRole(Constants.UNVERIFIED_USER_ROLE_ID);
                            var verifiedRole = guild.GetRole(Constants.VERIFIED_USER_ROLE_ID);

                            var user = guild.GetUserAsync(msg.Author.Id).Result;

                            SocketGuildUser socketGuildUser = user as SocketGuildUser;

                            if (socketGuildUser == null)
                            {
                                Logger.WriteDebug("DiscordVerify returning because socketGuildUser is null: " + msg.Author.Username);
                                return;
                            }

                            socketGuildUser.AddRoleAsync(verifiedRole).Wait();
                            Globals.DiscordUsers[msg.Author.Id].Roles.Add(verifiedRole.Id);
                            Logger.WriteDebug("DiscordVerify Added Verified to User: " + msg.Author.Username);

                            socketGuildUser.RemoveRoleAsync(unverifiedRole).Wait();
                            Globals.DiscordUsers[msg.Author.Id].Roles.Remove(unverifiedRole.Id);
                            Logger.WriteDebug("DiscordVerify removed Unverified from User: " + msg.Author.Username);

                            if(!string.IsNullOrEmpty(Globals.DiscordUsers[msg.Author.Id].KickReason))
                            {
                                // This will only work if user was kicked previously. It's OK for now
                                // TODO: Try to come up with a better way to handle this
                                DiscordResponse.Reply(msg, text: "Welcome back <@" + msg.Author.Id + ">. You're now verified.");
                                Logger.WriteDebug("DiscordVerify welcomed back returning user: " + msg.Author.Username);
                            }
                            else
                            {
                                // Assume brand new user
                                DiscordResponse.Reply(msg, text: "/welcome <@" + msg.Author.Id + ">");
                                Logger.WriteDebug("DiscordVerify welcomed new user: " + msg.Author.Username);
                            }
                        }
                    }
				}
            }
            catch(Exception ex)
            {
                Logger.HandleException(ex, "DiscordVerify:Exception:");
            }
        }
    }
}