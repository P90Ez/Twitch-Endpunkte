using Newtonsoft.Json;
using System;
using System.Net.Http;

namespace Twitch_API
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Twitch_API api = new Twitch_API("CLIENTID", "APP TOKEN");
            //Beispiele
            //Kanal nach Namen suchen -> kann verwendet werden um die ChannelID zu erhalten
            SearchChannels channel = api.SearchChannels("p90ez");
            if (channel != null)
                Console.WriteLine(channel.title);
            //Kanalinfo (bzw. Titel) aktualisieren
            if (api.SetChannelInformation(channel.id, new ChannelInformationModify { title = "Das hier hab ich mit der API aktualisiert!" }))
                Console.WriteLine("Kanalinformation erfolgreich aktualisiert!");
            //Neuen Titel mittels API abfragen
            ChannelInformation kanalinfo = api.GetChannelInformation(channel.id);
            Console.WriteLine($"Der aktuelle Titel lautet: {kanalinfo.title}");
        }
    }
}
