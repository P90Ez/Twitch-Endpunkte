using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Twitch_IRC
{
    internal class IRC_Controller
    {
        string _nick; //Nickname vom Bot
        string _oauth; //OAuth vom Bot
        string _channel; //Channel von welchem wir den Chat haben wollen
        string _server = "irc.chat.twitch.tv";
        int _port = 6667;
        private NetworkStream _stream;
        private TcpClient _irc;
        private StreamReader _reader;
        private StreamWriter _writer;
        public IRC_Controller(string nick, string oath, string channel)
        {
            _nick = nick;
            _oauth = oath;
            _channel = channel;
            Program.MessageSent += ChatWriter;
            Start();
        }
        private void Start()
        {
            do
            {
                try
                {
                    using (_irc = new TcpClient(_server, _port))
                    using (_stream = _irc.GetStream())
                    using (_reader = new StreamReader(_stream))
                    using (_writer = new StreamWriter(_stream))
                    {
                        IRCWriter($"PASS {_oauth}");
                        IRCWriter($"NICK {_nick}");
                        //IRCWriter("CAP REQ :twitch.tv/tags twitch.tv/commands twitch.tv/membership"); //tags, commands, membership anfordern
                        string inputline = "";
                        while((inputline = _reader.ReadLine()) != null)
                        {
                            Console.WriteLine("-> " + inputline); //Eingangsnachricht in der Console loggen
                            string[] splitinput = inputline.Split(' '); //Bei jedem Leerzeichen aufsplitten
                            if (splitinput[0] == "PING")
                                IRCWriter("PONG :tmi.twitch.tv");
                            switch (splitinput[1])
                            {
                                case "001": //001 = Erfolgreich verbunden -> dem Chat des Kanal joinen
                                    IRCWriter($"JOIN #{_channel}");
                                    break;
                                case "PRIVMSG": //:<user>!<user>@<user>.tmi.twitch.tv PRIVMSG #<channel> :This is a sample message
                                    string user = splitinput[0].Split('!')[0].Remove(0, 1); //funktioniert nur wenn tags, commands und membership nicht angefordert wurden
                                    string message = inputline.Split($"#{_channel} :")[1]; //funktioniert nur wenn tags, commands und membership nicht angefordert wurden
                                    OnMessageRecieved(user, message); //Eventhandler triggern
                                    break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(5000); //Bei Fehler nach x Sekunden Reconnect versuchen
                }
            } while (true);
        }
        /// <summary>
        /// Sendet eine IRC Nachricht an den Server. Dokumentation für Format beachten!
        /// </summary>
        /// <param name="text">Rohe IRC Nachricht</param>
        private void IRCWriter(string text)
        {
            if(text != "")
            try
            {
                _writer.WriteLine(text);
                _writer.Flush();
                Console.WriteLine("<- " + text); //Ausgangsnachricht in der Console loggen
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        /// <summary>
        /// Sendet eine Nachricht in den Twitch Chat, passendes IRC Format bereits vorhanden.
        /// </summary>
        /// <param name="text">Chatnachricht</param>
        private void ChatWriter(string text)
        {
            if (text != "")
                IRCWriter($"PRIVMSG #{_channel} :{text}"); //Format laut Twitch Dokumentation
        }
        private void ChatWriter(object source, MessageSentArgs args)
        {
            if (args.message != "")
                IRCWriter($"PRIVMSG #{_channel} :{args.message}"); //Format laut Twitch Dokumentation
        }
        /// <summary>
        /// Wird ausgelöst wenn jemand eine Nachricht in den Chat schreibt.
        /// </summary>
        public static EventHandler<MessageRecievedArgs> MessageRecieved;
        protected virtual void OnMessageRecieved(string username, string message)
        {
            MessageRecieved?.Invoke(this, new MessageRecievedArgs(username,message));
        }
    }
    class MessageRecievedArgs
    {
        public string username { get; }
        public string message { get; }
        public MessageRecievedArgs(string username, string message)
        {
            this.username = username;
            this.message = message;
        }
    }
}
