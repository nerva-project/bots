using System;
using System.Collections.Generic;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Nerva.Bots.Helpers;
using Nerva.Bots.Plugin;

namespace Nerva.Bots.Classes
{
    public static class Globals
    {
        public static Assembly BotAssembly { get; set; } = null;

		public static IBot Bot { get; set; } = null;

        public static DiscordSocketClient Client { get; set; }

		public static Nerva.Rpc.Log RpcLogConfig { get; set; } = Nerva.Rpc.Log.Presets.None;

		public static Dictionary<string, string> BotHelp { get; set; } = new Dictionary<string, string>();

		public static Dictionary<string, Type> Commands = new Dictionary<string, Type>();

		public static IDictionary<ulong, DiscordUser> DiscordUsers = new Dictionary<ulong, DiscordUser>();
        public static bool IsUserDictionaryChanged = false;

        public static void SyncUsersFromDiscord()
        {
            try
            {
                IGuild guild = Globals.Client.GetGuild(Globals.Bot.Config.ServerId);
                var users = guild.GetUsersAsync(CacheMode.AllowDownload).Result;
                
                // Loop through users and load them to user object
                foreach (var user in users)
                {
                    SocketGuildUser socketUser = (SocketGuildUser)user;
                    IEnumerable<SocketRole> userRoles = socketUser.Roles;

                    if(!socketUser.IsBot)
                    {
                        if(!Globals.DiscordUsers.ContainsKey(socketUser.Id))
                        {
                            Globals.AddUserToDictionary(socketUser);								
                        }
                    }
                
                    //Logger.WriteDebug("User Name: " + socketUser.Username + " | Discriminator: " + socketUser.Discriminator + " | Id: " + socketUser.Id + " | Joined: " + socketUser.JoinedAt.ToString() + " | IsBot: " + socketUser.IsBot + " | Roles: " + stringRoles);					
                }
            }
            catch (Exception ex)
            {
                Logger.HandleException(ex, "SynchUsersFromDiscord: ");
            }
        }
        public static void AddUserToDictionary(IUser user)
		{
			try
			{
				if(!Globals.DiscordUsers.ContainsKey(user.Id))
				{
					SocketGuildUser socketUser = (SocketGuildUser)user;
					IEnumerable<SocketRole> userRoles = socketUser.Roles;

					// Add new user to dictionary
					DiscordUser newUser = new DiscordUser();
					newUser.Id = user.Id;
					newUser.UserName = user.Username;
					newUser.Discriminator = user.Discriminator;
					// JoinedDate will be inaccurate for some calls to this methods as it will be Discord joined date.
					// For now, this will be set in SyncRolesWithDiscord()
					//newUser.JoinedDate = user.CreatedAt.DateTime;

					newUser.Roles = new List<ulong>();
					foreach(SocketRole role in userRoles)
					{
						newUser.Roles.Add(role.Id);
					}

					Globals.DiscordUsers.Add(user.Id, newUser);
				}
			}
			catch (Exception ex)
			{
				Logger.HandleException(ex, "AddUserToDictionary: ");
			}
		}

        public static void SyncRolesFromDiscord()
		{
			try
			{
				// This will only synchronize Verified and Unverified roles as those are the only ones we need
				SocketGuild guild = Globals.Client.GetGuild(Globals.Bot.Config.ServerId);
				SocketRole roleUnverified = guild.GetRole(Constants.UNVERIFIED_USER_ROLE_ID);
				SocketRole roleVerified = guild.GetRole(Constants.VERIFIED_USER_ROLE_ID);

				foreach(DiscordUser dictionaryUser in Globals.DiscordUsers.Values)
				{
					// Update user JoinedDate if different in Discord
					foreach(SocketGuildUser socketGuildUser in roleUnverified.Members)
					{
						if(socketGuildUser.Id == dictionaryUser.Id)
						{
							if(dictionaryUser.JoinedDate != socketGuildUser.JoinedAt.Value.DateTime)
							{
								dictionaryUser.JoinedDate = socketGuildUser.JoinedAt.Value.DateTime;
								Logger.WriteDebug("SyncRolesWithDiscord changed Guild Joined for Unverified User: " + dictionaryUser.UserName + " | New Date: " + dictionaryUser.JoinedDate.ToString());
							}
						}
					}

					// Need to do this for Verified users as well or it might kick them too soon
					foreach(SocketGuildUser socketGuildUser in roleVerified.Members)
					{
						if(socketGuildUser.Id == dictionaryUser.Id)
						{
							if(dictionaryUser.JoinedDate != socketGuildUser.JoinedAt.Value.DateTime)
							{
								dictionaryUser.JoinedDate = socketGuildUser.JoinedAt.Value.DateTime;
								Logger.WriteDebug("SyncRolesWithDiscord changed Guild Joined for Verified User: " + dictionaryUser.UserName + " | New Date: " + dictionaryUser.JoinedDate.ToString());
							}
						}
					}


					bool userUnverifiedInDiscord = false;
					bool userVerifiedInDiscord = false;

					// Check if user has Unverified role in Discord
					foreach(SocketGuildUser socketUser in roleUnverified.Members)
					{
						if(socketUser.Id == dictionaryUser.Id)
						{
							userUnverifiedInDiscord = true;
                            break;
						}
					}

					// Check if user has Verified role in Discord
					foreach(SocketGuildUser socketUser in roleVerified.Members)
					{
						if(socketUser.Id == dictionaryUser.Id)
						{
							userVerifiedInDiscord = true;
                            break;
						}
					}


					// Synchronize "Unverified" role
					if(userUnverifiedInDiscord)
					{
						// User has "Unverified" role in Discord
						if(!dictionaryUser.Roles.Contains(Constants.UNVERIFIED_USER_ROLE_ID))
						{
							// But does not have "Unverified" role in Dictionary. Add role
							dictionaryUser.Roles.Add(Constants.UNVERIFIED_USER_ROLE_ID);
							if(!IsUserDictionaryChanged) IsUserDictionaryChanged = true;
							Logger.WriteDebug("SyncRolesWithDiscord added Unverified role to User: " + dictionaryUser.UserName);
						}
					}
					else 
					{
						// User does not have "Unverified" role in Discord
						if(dictionaryUser.Roles.Contains(Constants.UNVERIFIED_USER_ROLE_ID))
						{
							// But "Unverified" in dictionary. Remove role
							dictionaryUser.Roles.Remove(Constants.UNVERIFIED_USER_ROLE_ID);
							if(!IsUserDictionaryChanged) IsUserDictionaryChanged = true;
							Logger.WriteDebug("SyncRolesWithDiscord removed Unverified role from User: " + dictionaryUser.UserName);
						}
					}

					// Synchronize "Verified" role
					if(userVerifiedInDiscord)
					{
						// User has "Verified" role in Discord
						if(!dictionaryUser.Roles.Contains(Constants.VERIFIED_USER_ROLE_ID))
						{
							// But does not have "Verified" role in Dictionary. Add role
							dictionaryUser.Roles.Add(Constants.VERIFIED_USER_ROLE_ID);
							if(!IsUserDictionaryChanged) IsUserDictionaryChanged = true;
							Logger.WriteDebug("SyncRolesWithDiscord added Verified role to User: " + dictionaryUser.UserName);
						}
					}
					else 
					{
						// User does not have "Verified" role in Discord
						if(dictionaryUser.Roles.Contains(Constants.VERIFIED_USER_ROLE_ID))
						{
							// But has "Verified" in dictionary. Remove role
							dictionaryUser.Roles.Remove(Constants.VERIFIED_USER_ROLE_ID);
							if(!IsUserDictionaryChanged) IsUserDictionaryChanged = true;
							Logger.WriteDebug("SyncRolesWithDiscord removed Verified role from User: " + dictionaryUser.UserName);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.HandleException(ex, "SyncRolesWithDiscord: ");
			}	
		}

        public static void ResetUserWarnedDate(DiscordUser user)
        {
            try
            {
                Logger.WriteDebug("Resetting warned date for user: " + user.UserName);
                user.WarnedDate = DateTime.MinValue;
                Globals.IsUserDictionaryChanged = true;
            }
            catch (Exception ex)
            {
                Logger.HandleException(ex, "ResetUserWarnedDate: ");
            }
        }

        public static void ResetUserKickedDate(DiscordUser user)
        {
            try
            {
                Logger.WriteDebug("Resetting kick date for user: " + user.UserName);
                user.KickDate = DateTime.MinValue;
                Globals.IsUserDictionaryChanged = true;
            }
            catch (Exception ex)
            {
                Logger.HandleException(ex, "ResetUserKickedDate: ");
            }
        }
    }
}