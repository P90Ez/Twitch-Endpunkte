using System;
using System.Collections.Generic;
using System.Text;

namespace Twitch_Pubsub.BitsBadges
{
    public class Message
    {
        public string user_id { get; set; }
        public string user_name { get; set; }
        public string channel_id { get; set; }
        public string channel_name { get; set; }
        public int badge_tier { get; set; }
        public string chat_message { get; set; }
        public string time { get; set; }
    }
}
