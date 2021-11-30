using System;
using System.Collections.Generic;
using System.Text;

namespace Twitch_Pubsub.Bits
{
    public class BadgeEntitlement
    {
        public int new_version { get; set; }
        public int previous_version { get; set; }
    }
    public class Data
    {
        public string user_name { get; set; }
        public string channel_name { get; set; }
        public string user_id { get; set; }
        public string channel_id { get; set; }
        public DateTime time { get; set; }
        public string chat_message { get; set; }
        public int bits_used { get; set; }
        public int total_bits_used { get; set; }
        public string context { get; set; }
        public BadgeEntitlement badge_entitlement { get; set; }
    }
    public class Message
    {
        public Data data { get; set; }
        public string version { get; set; }
        public string message_type { get; set; }
        public string message_id { get; set; }
        public bool is_anonymous { get; set; }
    }
}
