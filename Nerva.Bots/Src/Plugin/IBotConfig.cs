using System.Collections.Generic;

namespace Nerva.Bots.Plugin
{
    public interface IBotConfig
    {
        //Id of the bot user
        ulong BotId { get; }

        ulong ServerOwnerId { get; }
        
        ulong ServerId { get; }

        List<ulong> ServerAdminRoleIds { get; }

        List<ulong> BotChannelIds { get; }

        //Anyone in one of these roles can issue commands outside the restricted channels
        List<ulong> BotCommanderRoleIds { get; }
        
		string CmdPrefix { get; }
    }
}