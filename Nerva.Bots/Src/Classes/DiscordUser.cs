using System;
using System.Collections.Generic;

namespace Nerva.Bots.Classes
{
    public class DiscordUser
    {
        public string UserName { get; set; }
        public string Discriminator { get; set; }
        public IList<string> Roles { get; set; }
        public DateTime LastPostDate { get; set; }
        public DateTime WarnedDate { get; set; }
    }
}