using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using WebSocketSharp;
using System.Linq;

namespace Twitch_Pubsub
{
    internal class PubSub
    {
        //Vars
        protected WebSocket _Socket;
        protected Dictionary<topics, string> _Eventtopics;
        protected string _authToken { get; }
        protected string _channelID { get; }
        protected string _nounce { get; }
        public enum topics
        {
            bits,
            bitsBadges,
            channelPoints,
            channelSubscritions,
            whispers
        }
        //EventHandler
        /// <summary>
        /// Wird getriggert wenn Kanalpunkte eingelöst werden
        /// </summary>
        public EventHandler<ChannelPoints.Message> ChannelPointsRedeemed;
        protected virtual void OnChannelPointsRedeemed(ChannelPoints.Message message)
        {
            ChannelPointsRedeemed?.Invoke(this, message);
        }
        /// <summary>
        /// Wird getriggert wenn Bits eingelöst werden
        /// </summary>
        public EventHandler<Bits.Message> BitsRedeemed;
        protected virtual void OnBitsRedeemed(Bits.Message message)
        {
            BitsRedeemed?.Invoke(this,message);
        }
        /// <summary>
        /// Wird getriggert wenn jemand ein neues Badge erhält
        /// </summary>
        public EventHandler<BitsBadges.Message> BitsBadgeUpdated;
        protected virtual void OnBitsBadgeUpdated(BitsBadges.Message message)
        {
            BitsBadgeUpdated?.Invoke(this, message);
        }
        /// <summary>
        /// Wird getriggert wenn jemand Subt/Resub/Subs verschenkt
        /// </summary>
        public EventHandler<Subscriptions.Message> Subscribed;
        protected virtual void OnSubscribed(Subscriptions.Message message)
        {
            Subscribed?.Invoke(this, message);
        }
        /// <summary>
        /// Wird getriggert wenn jemand dem Kanal whispert
        /// </summary>
        public EventHandler<Whispers.Message> Whisper;
        protected virtual void OnWhisper(Whispers.Message message)
        {
            Whisper?.Invoke(this, message);
        }

        //Konstruktor
        /// <summary>
        /// Erstellt eine neue Instanz des Objekts & stellt eine Verbindung zum Endpunkt her
        /// </summary>
        /// <param name="authToken">AutToken, oder AppToken (kann auf folgender Website mit den Scopes lt. Dokumentation generiert werden: https://twitchapps.com/tokengen/)</param>
        /// <param name="ChannelID">ChannelID, kann durch API ermittelt werden</param>
        public PubSub(string authToken, string ChannelID)
        {
            _authToken = authToken;
            _channelID = ChannelID;
            _nounce = "optional"; //nicht zwingend notwendig
            _Eventtopics = new Dictionary<topics, string>()
            {
                { topics.bits, $"channel-bits-events-v2.{_channelID}" },                        //Bits
                { topics.bitsBadges, $"channel-bits-badge-unlocks.{_channelID}" },              //Bits Badge Notification
                { topics.channelPoints, $"channel-points-channel-v1.{_channelID}" },            //Channel Points
                { topics.channelSubscritions, $"channel-subscribe-events-v1.{_channelID}" },    //Channel Subscriptions
                { topics.whispers, $"whispers.{_channelID}" }                                   //Whispers
            };
            _Socket = new WebSocket("wss://pubsub-edge.twitch.tv");
            _Socket.OnOpen += SocketConnect;
            _Socket.OnMessage += SocketMessage;
            _Socket.Connect();
        }
        /// <summary>
        /// Wird beim Connecten aufgerufen, gibt dem Server bescheid welche Events abonniert werden sollen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SocketConnect(object sender, EventArgs args)
        {
            _Socket.Send(JsonConvert.SerializeObject(new Ping()));
            _Socket.Send(JsonConvert.SerializeObject(new Request()
            {
                type = "LISTEN",
                nonce = _nounce,
                data = new RequestData() { auth_token = _authToken, topics = _Eventtopics.Values.ToArray()},
            }));
            new Thread(new ThreadStart(SocketPinger)) { IsBackground = true }.Start();
        }
        /// <summary>
        /// Handlet alle Nachrichten und Events, welche vom Server gesendet werden
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SocketMessage(object sender, MessageEventArgs args)
        {
            try
            {
                Message message = JsonConvert.DeserializeObject<Message>(args.Data); //Standartnachricht deserialisieren
                if (message != null)
                    switch (message.type)
                    {
                        case "MESSAGE":
                            if (message.data != null)
                            {
                                if (message.data.topic == _Eventtopics[topics.bits])                         //BITS
                                {
                                    Bits.Message bitsmessage = JsonConvert.DeserializeObject<Bits.Message>(message.data.message);
                                    if (bitsmessage != null)
                                        if (bitsmessage.message_type == "bits_event")
                                            OnBitsRedeemed(bitsmessage);
                                }
                                else if (message.data.topic == _Eventtopics[topics.bitsBadges])             //BITS BADGES
                                {
                                    BitsBadges.Message bitsbadgesmessage = JsonConvert.DeserializeObject<BitsBadges.Message>(message.data.message);
                                    if (bitsbadgesmessage != null)
                                        OnBitsBadgeUpdated(bitsbadgesmessage);
                                }
                                else if (message.data.topic == _Eventtopics[topics.channelPoints])           //CHANNELPOINTS
                                {
                                    ChannelPoints.Message channelpointsmessage = JsonConvert.DeserializeObject<ChannelPoints.Message>(message.data.message);
                                    if (channelpointsmessage != null)
                                        if (channelpointsmessage.type == "reward-redeemed")
                                            OnChannelPointsRedeemed(channelpointsmessage);
                                }
                                else if (message.data.topic == _Eventtopics[topics.channelSubscritions])    //CHANNEL SUBSCRIPTIONS
                                {
                                    Subscriptions.Message submessage = JsonConvert.DeserializeObject<Subscriptions.Message>(message.data.message);
                                    if (submessage != null)
                                        OnSubscribed(submessage);
                                }
                                else if (message.data.topic == _Eventtopics[topics.whispers])               //WHISPERS
                                {
                                    try
                                    {
                                        Whispers.Message whispermessage = JsonConvert.DeserializeObject<Whispers.Message>(message.data.message);
                                        if (whispermessage != null)
                                            if (whispermessage.type != null)
                                                OnWhisper(whispermessage);
                                    }
                                    catch
                                    {
                                        //Jeder Whisper wird 2 mal gesendet, jedoch hat dieser Json String einen anderen aufbau, wodurch das Deserialisieren fehlschlägt.
                                    }
                                }
                            }
                            break;
                        case "PONG": //Antwort bei erfolgreichem Ping
                            Console.WriteLine("Ping erfolgreich!");
                            break;
                        case "RESPONSE": //Antwort auf Listen Anfrage
                            Response response = JsonConvert.DeserializeObject<Response>(args.Data); //Antwort deserialisieren
                            if (response != null)
                                if (response.error == "")
                                    Console.WriteLine("Erfolgreich verbunden!");
                                else
                                    Console.WriteLine("Fehler beim Verbinden: " + response.error);
                            break;
                        default:
                            break;

                    }
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message); //Wenn Fehler -> in Console ausgeben
            }
        }
        /// <summary>
        /// Websocket Server muss mindestens einmal alle 5 Minuten angepingt werden (Connection Management hier nicht inbegriffen)
        /// </summary>
        private void SocketPinger()
        {
            while (true)
            {
                try
                {
                    _Socket.Send(JsonConvert.SerializeObject(new Ping()));
                    Thread.Sleep(300000);
                }
                catch
                {

                }
            }
        }
    }
    //Klassen um Json Strings zu zerlegen oder zu bauen
    internal class Ping
    {
        public string type = "PING";
    }
    internal class Request
    {
        public string type { get; set; }
        public string nonce { get; set; }
        public RequestData data { get; set; }
    }
    internal class RequestData
    {
        public string[] topics { get; set; }
        public string auth_token { get; set; }
    }
    internal class Response
    {
        public string type { get; set; }
        public string nonce { get; set; }
        public string error { get; set; }
    }
    internal class Message
    {
        public string type { get; set; }
        public MessageData data { get; set; }
    }
    internal class MessageData
    {
        public string topic { get; set; }
        public string message { get; set; }
    }
}
