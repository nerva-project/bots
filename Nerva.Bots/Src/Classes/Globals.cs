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
    }
}