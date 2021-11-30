using System;
using System.Collections.Generic;
using System.Text;

namespace Twitch_Pubsub.Whispers
{
    public class Tags
    {
        public string login { get; set; }
        public string display_name { get; set; }
        public string color { get; set; }
        public List<object> emotes { get; set; }
        public List<object> badges { get; set; }
    }

    public class Recipient
    {
        public int id { get; set; }
        public string username { get; set; }
        public string display_name { get; set; }
        public string color { get; set; }
    }

    public class DataObject
    {
        public string message_id { get; set; }
        public int id { get; set; }
        public string thread_id { get; set; }
        public string body { get; set; }
        public int sent_ts { get; set; }
        public int from_id { get; set; }
        public Tags tags { get; set; }
        public Recipient recipient { get; set; }
        public string nonce { get; set; }
    }

    public class Message
    {
        public string type { get; set; }
        public string data { get; set; }
        public DataObject data_object { get; set; }
    }
}
