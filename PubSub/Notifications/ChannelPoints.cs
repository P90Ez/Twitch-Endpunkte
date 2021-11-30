﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Twitch_Pubsub.ChannelPoints
{
    public class Message
    {
        public string type { get; set; }
        public Data data { get; set; }
    }
    public class Data
    {
        public string timestamp { get; set; }
        public Redemption redemption { get; set; }
    }
    public class Redemption
    {
        public string id { get; set; }
        public User user { get; set; }
        public string channel_id { get; set; }
        public string redeemed_at { get; set; }
        public Reward reward { get; set; }
        public string user_input { get; set; }
        public string status { get; set; }
    }
    public class Reward
    {
        public string id { get; set; }
        public string channel_id { get; set; }
        public string title { get; set; }
        public string prompt { get; set; }
        public int cost { get; set; }
        public bool is_user_input_required { get; set; }
        public bool is_sub_only { get; set; }
        public Image image { get; set; }
        public DefaultImage default_image { get; set; }
        public string background_color { get; set; }
        public bool is_enabled { get; set; }
        public bool is_paused { get; set; }
        public bool is_in_stock { get; set; }
        public MaxPerStream max_per_stream { get; set; }
        public bool should_redemptions_skip_request_queue { get; set; }
    }
    public class User
    {
        public string id { get; set; }
        public string login { get; set; }
        public string display_name { get; set; }
    }
    public class Image
    {
        public string url_1x { get; set; }
        public string url_2x { get; set; }
        public string url_4x { get; set; }
    }
    public class DefaultImage
    {
        public string url_1x { get; set; }
        public string url_2x { get; set; }
        public string url_4x { get; set; }
    }
    public class MaxPerStream
    {
        public bool is_enabled { get; set; }
        public int max_per_stream { get; set; }
    }
}
