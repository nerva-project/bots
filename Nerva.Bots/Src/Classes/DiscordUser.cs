using System;
using System.Collections.Generic;

namespace Nerva.Bots.Classes
{
    public class DiscordUser
    {
        public ulong Id { get; set; }
        public string UserName { get; set; }
        public string Discriminator { get; set; }
        public DateTime JoinedDate { get; set; }
        public IList<ulong> Roles { get; set; }
        public DateTime LastPostDate { get; set; }
        public DateTime WarnedDate { get; set; }
    }
}