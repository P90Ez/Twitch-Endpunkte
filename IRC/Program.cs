using System;

namespace Twitch_IRC
{
    internal class Program
    {
        //Main
        static void Main(string[] args)
        {
            IRC_Controller.MessageRecieved += new Program().KeksCommand;
            IRC_Controller controller = new IRC_Controller("NICK", "OAUTH", "CHANNEL");
        }
        //Send Message Event
        /// <summary>
        /// Wird ausgelöst wenn der Bot eine Nachricht in den Chat schreiben soll.
        /// </summary>
        public static EventHandler<MessageSentArgs> MessageSent;
        protected virtual void OnMessageSent(string message)
        {
            MessageSent?.Invoke(this, new MessageSentArgs(message));
        }
        //!Kekse
        public void KeksCommand(object source, MessageRecievedArgs args)
        {
            if (args.message.Split(' ')[0].ToLower() == "!kekse")
                OnMessageSent($"/me gibt @{args.username} 4 Kekse.");
        }
    }
    class MessageSentArgs
    {
        public string message { get; }
        public MessageSentArgs(string message)
        {
            this.message = message;
        }
    }
}
