using System;
using System.Collections.Generic;
using System.Text;

namespace Twitch_Pubsub.Subscriptions
{
    public class Emote
    {
        public int start { get; set; }
        public int end { get; set; }
        public int id { get; set; }
    }
    public class SubMessage
    {
        public string message { get; set; }
        public List<Emote> emotes { get; set; }
    }
    public class Message
    {
        public string user_name { get; set; }
        public string display_name { get; set; }
        public string channel_name { get; set; }
        public string user_id { get; set; }
        public string channel_id { get; set; }
        public DateTime time { get; set; }
        public string sub_plan { get; set; }
        public string sub_plan_name { get; set; }
        public int cumulative_months { get; set; }
        public int streak_months { get; set; }
        public string context { get; set; }
        public bool is_gift { get; set; }
        public SubMessage sub_message { get; set; }
        public string recipient_id { get; set; }
        public string recipient_user_name { get; set; }
        public string recipient_display_name { get; set; }
        public string multi_month_duration { get; set; }
    }
}
