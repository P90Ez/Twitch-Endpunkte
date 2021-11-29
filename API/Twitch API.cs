using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Twitch_API
{
    internal class Twitch_API
    {
        protected string _authToken;
        protected string _clientId;
        public Twitch_API(string clientID, string authToken)
        {
            _authToken = authToken;
            _clientId = clientID;
        }
        /// <summary>
        /// Fragt bei der Twitch API an
        /// </summary>
        /// <param name="url">API URL https://api.twitch.tv/helix/[URL]</param>
        /// <param name="method">Art des Requests (für jeden Zugriff in der Dokumentation zu finden!)</param>
        /// <param name="application">Request Body</param>
        /// <returns>Gibt den Body der Antwort zurück, oder null wenn die Anfrage fehlschlägt</returns>
        private string makeRequest(string url, HttpMethod method, string application = "")
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_authToken}"); //Autorisierung
                    client.DefaultRequestHeaders.Add("Client-Id", _clientId); //Autorisierung
                    var request = new HttpRequestMessage
                    {
                        Method = method, //Art der Anfrage festlegen
                        RequestUri = new Uri($"https://api.twitch.tv/helix/{url}"), //URI, bzw. URL festlegen
                        Content = new StringContent(application, Encoding.UTF8, "application/json") //Content (bzw. Body) der Anfrage festlegen
                    };
                    var response = client.SendAsync(request).Result; //Antwort abwarten
                    return response.Content.ReadAsStringAsync().Result; //Antwort zu String umwandeln
                }
            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return null; //Gibt null zurück wenn die Anfrage fehlschlägt
        }

        //Beispiele:

        /// <summary>
        /// Sucht nach einen bestimmten Kanal
        /// </summary>
        /// <param name="channelName">Name des Kanals</param>
        /// <returns>Gibt null zurück wenn der Kanal nicht gefunden werden kann</returns>
        public SearchChannels SearchChannels(string channelName)
        {
            SearchChannelsResponse response = JsonConvert.DeserializeObject <SearchChannelsResponse> (makeRequest($"search/channels?query={channelName.ToLower()}", HttpMethod.Get));
            if(response != null)
            foreach(var channel in response.data) //Kanal in der Liste suchen
                {
                if (channel.broadcaster_login.ToLower() == channelName.ToLower() | channel.display_name.ToLower() == channelName.ToLower())
                    return channel;
            }
            return null; //gibt nur dann null zurück wenn der Kanal nicht gefunden werden kann
        }
        /// <summary>
        /// Gibt Informationen zu einem beliebigen Kanal zurück
        /// </summary>
        /// <param name="channelID"></param>
        /// <returns></returns>
        public ChannelInformation GetChannelInformation(string channelID)
        {
            ChannelInformationRespone response = JsonConvert.DeserializeObject<ChannelInformationRespone>(makeRequest($"channels?broadcaster_id={channelID}", HttpMethod.Get));
            return response.data[0];
        }
        /// <summary>
        /// Modifiziert Kanalinformationen
        /// </summary>
        /// <param name="channelID"></param>
        /// <param name="information"></param>
        /// <returns>Gibt true zurück wenn erfolgreich</returns>
        public bool SetChannelInformation(string channelID, ChannelInformationModify information)
        {
            string response = makeRequest($"channels?broadcaster_id={channelID}", HttpMethod.Patch, JsonConvert.SerializeObject(information));
            return response != null; //true wenn Erfolgreich
        }
    }
    //Klassen für Beispiele
    internal class ChannelInformationRespone
    {
        public List<ChannelInformation> data { get; set; }
    }
    public class ChannelInformation
    {
        public string broadcaster_id { get; set; }
        public string broadcaster_login { get; set; }
        public string broadcaster_name { get; set; }
        public string broadcaster_language { get; set; }
        public string game_id { get; set; }
        public string game_name { get; set; }
        public string title { get; set; }
        public int delay { get; set; }
    }
    public class ChannelInformationModify
    {
        public string game_id { get; set; }
        public string broadcaster_language { get; set; }
        public string title { get; set; }
    }
    internal class SearchChannelsResponse
    {
        public List<SearchChannels> data { get; set; }
    }
    public class SearchChannels
    {
        public string broadcaster_language { get; set; }
        public string broadcaster_login { get; set; }
        public string display_name { get; set; }
        public string game_id { get; set; }
        public string game_name { get; set; }
        public string id { get; set; }
        public bool is_live { get; set; }
        public List<object> tags_ids { get; set; }
        public string thumbnail_url { get; set; }
        public string title { get; set; }
        public string started_at { get; set; }
    }
}
