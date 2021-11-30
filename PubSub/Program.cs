using System;

namespace Twitch_Pubsub
{
    internal class Program
    {
        static void Main(string[] args)
        {
            PubSub pubSub = new PubSub("AUTHTOKEN", "CHANNELID");       //AuthToken bzw. AppToken generieren: https://twitchapps.com/tokengen/ (Scopes lt. Dokumentation: https://dev.twitch.tv/docs/pubsub#topics)
                                                                        //ChannelID kann durch API ermittelt werden
            pubSub.BitsRedeemed += OnBitsRedeemed;
            pubSub.BitsBadgeUpdated += OnBitsBadgeUpdated;
            pubSub.ChannelPointsRedeemed += OnChannelPointsRedeemed;
            pubSub.Subscribed += OnSubscribed;
            pubSub.Whisper += OnWhisper;
            Console.ReadLine();
        }
        //Beispiele
        private static void OnBitsRedeemed(object sender, Bits.Message message)
        {
            if(!message.is_anonymous)
                Console.WriteLine($"[Bits] {message.data.user_name} cheered {message.data.bits_used} Bits!");
            else
                Console.WriteLine($"[Bits] Anonymous cheered {message.data.bits_used} Bits!");
        }
        private static void OnBitsBadgeUpdated(object sender, BitsBadges.Message message)
        {
            Console.WriteLine($"[BitsBadge] {message.user_name} hat eine neues Badge Tier erhalten: {message.badge_tier}");
        }
        private static void OnChannelPointsRedeemed(object sender, ChannelPoints.Message message)
        {
            Console.WriteLine($"[ChannelPoints] {message.data.redemption.user.display_name} hat {message.data.redemption.reward.title} erhalten!");
        }
        private static void OnSubscribed(object sender, Subscriptions.Message message)
        {
            switch (message.context)
            {
                case "sub":
                    Console.WriteLine($"[Subscriptions] {message.display_name} hat gerade abonniert!");
                    break;
                case "resub":
                    Console.WriteLine($"[Subscriptions] {message.display_name} hat gerade im {message.cumulative_months} Monat abonniert!");
                    break;
                case "subgift":
                case "resubgift":
                    Console.WriteLine($"[Subscriptions] {message.display_name} verschenkt einen Sub an {message.recipient_display_name}!");
                    break;
                case "anonsubgift":
                case "anonresubgift":
                    Console.WriteLine($"[Subscriptions] {message.recipient_display_name} hat einen Sub von Anonym erhalten!");
                    break;
                default:
                    break;
            }
        }
        private static void OnWhisper(object sender, Whispers.Message message)
        {
            Console.WriteLine($"[Whisper] {message.data_object.recipient.username}: {message.data_object.body}");
        }
    }
}
