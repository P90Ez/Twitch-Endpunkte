using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;

namespace Tokens_Generieren
{
    internal class Program
    {
        static void Main(string[] args)
        {
            /* USAGE / INFO
             * 
             * There are two types of tokens: user acces tokens and app acces tokens
             * 
             * User Acces Tokens:
             * Can be obtained by using ImplicitGrantFlow (if your app DOES NOT use a server) and AuthorizationCodeFlow (if your app DOES use a server)
             * These tokens can be used to get information about the user. The scopes provided define how much information you can get/how much you can do.
             * You can't use User Acces Tokens to call the EventSub APIs (App Acces Token is needed)
             * 
             * App Acces Tokens:
             * Can be obtained by using ClientCredentialsGrantFlow.
             * You should get an app access token, if your app only calls APIs that don’t require the user’s permission to access the resource.
             * 
             * RefreshToken:
             * If your token is expired (API returns 401 Unauthorized) you need to refresh your token.
             * The method RefreshToken can only be used if you used AuthorizationCodeFlow to obtain your token. If you used ImplicitGrantFlow or ClientCredentialsGrantFlow you need to use these again to "refresh" your token.
             * 
             * IsTokenValid:
             * Can be used with any type of token to check if it is still valid. The "tokentype" is provided alongside the token when using any of the three methods to get your token.
             * 
             * OAuth Redirect URL:
             * The redirect url of your app (in the twitch dev console) needs to be set to "http://localhost:9154" - you can change the port used inside the GetToken class using the static port variable.
             *
             * Here is an example on using these methods.
             */

            //User Acces Tokens:
            //ImplicitGrantFlow - App without server (front-end only)
            var ImplicitGrantFlowCreds = GetToken.ImplicitGrantFlow("CLIENT_ID", new List<string>() { "SCOPES" });
            //AuthorizationCodeFlow - App with server (be careful, you can't just copy this method into your front-end - this will expose your client secret. The first half goes into the front-end, the other half into your back-end)
            var AuthorizationCodeFlowCreds = GetToken.AuthorizationCodeFlow("CLIENT_ID", "CLIENT_SECRET", new List<string>() { "SCOPES" });
            var RefreshedCreds = GetToken.RefreshToken(AuthorizationCodeFlowCreds, "CLIENT_SECRET");

            //App Acces Tokens:
            //ClientCredentialsGrantFlow - back-end only
            var ClientCredentialsGrantFlowCreds = GetToken.ClientCredentialsGrantFlow("CLIENT_ID", "CLIENT_SECRET");

            //Validate Token:
            bool isValid = GetToken.IsTokenValid("TOKEN", "TOKEN_TYPE");
        }
    }
    internal class GetToken
    {
        private static int port = 9154; //has to be the same as in your app's registered URI - using the standard 3000/3030 may cause some troubles with other apps on your device

        #region ImplicitGrantFlow
        /// <summary>
        /// This flow is meant for apps that don’t use a server, such as client-side JavaScript apps or mobile apps.
        /// </summary>
        public static Credentials ImplicitGrantFlow(string ClientId, List<string> Scopes)
        {
            string redirecturl = $"http://localhost:{port}"; //Your app’s registered redirect URI
            Random rnd = new Random();
            string state = rnd.Next().ToString("x"); //generate a validation string to prevent CSRF attacks
            string url = "https://id.twitch.tv/oauth2/authorize" + //generate URL with parameters - these parameters can be found in the documentation
                "?response_type=token" +
                $"&client_id={ClientId}" +
                $"&redirect_uri={redirecturl}" +
                $"&{GenerateScopes(Scopes)}" +
                $"&state={state}";
            OpenBrowser(url); //Open URL in browser
            var paras = GetUrlParams(Listener(redirecturl)); //gets & parses url parameters after user authorization
            if(!ImplicitGrantFlow_CheckParas(paras, state)) return new Credentials("Para Check failed"); //check parameters
            var valiresponse = ValidateToken(paras["token_type"], paras["access_token"]); //Validate token using another request to twitch server
            if (!CheckValidationRequest(valiresponse)) return new Credentials("Token Validation Failed"); //Check validation response
            try
            {
                var creds = JsonConvert.DeserializeObject<Credentials>(valiresponse.Content.ReadAsStringAsync().Result); //Deserialize validation response
                creds.isSucces = true;
                creds.AuthToken = paras["access_token"];
                return creds;
            }
            catch { return new Credentials("Credential Deserialization Failed"); }
        }
        /// <summary>
        /// Checks the parameter returned by the browser
        /// </summary>
        private static bool ImplicitGrantFlow_CheckParas(Dictionary<string, StringValues> paras, string state)
        {
            if(paras == null) return false;
            if (!paras.ContainsKey("state")) return false;
            if (paras["state"] != state) return false;
            if (!paras.ContainsKey("access_token") || paras["access_token"] == "") return false;
            if (!paras.ContainsKey("token_type") || paras["token_type"] == "") return false;
            return true;
        }
        #endregion
        #region AuthorizationCodeFlow
        /// <summary>
        /// This flow is meant for apps that use a server, can securely store a client secret, and can make server-to-server requests to the Twitch API.
        /// </summary>
        public static Credentials AuthorizationCodeFlow(string ClientId, string ClientSecret, List<string> Scopes)
        {
            string redirecturl = $"http://localhost:{port}"; //Your app’s registered redirect URI
            Random rnd = new Random();
            string state = rnd.Next().ToString("x"); //generate a validation string to prevent CSRF attacks
            string url = "https://id.twitch.tv/oauth2/authorize" + //generate URL with parameters - these parameters can be found in the documentation
                "?response_type=code" +
                $"&client_id={ClientId}" +
                $"&redirect_uri={redirecturl}" +
                $"&{GenerateScopes(Scopes)}" +
                $"&state={state}";
            OpenBrowser(url); //Open URL in browser
            var paras = GetUrlParams(Listener(redirecturl)); //gets & parses url parameters after user authorization
            if (!AuthorizationCodeFlow_CheckParas(paras, state)) return new Credentials("Para Check failed"); //check parameters - These parameters only contain a code. This code has to be used in the next step to get an acces token.
            //Create request body - parameters can be found in the documentation
            var application = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("client_id", ClientId), new KeyValuePair<string, string>("client_secret", ClientSecret), new KeyValuePair<string, string>("code", paras["code"]), new KeyValuePair<string, string>("grant_type", "authorization_code"), new KeyValuePair<string, string>("redirect_uri", redirecturl) };
            var tokenresponse = HeaderlessURLEncodedRequest("https://id.twitch.tv/oauth2/token", application, HttpMethod.Post); //Send request to optain the acces token
            if (!tokenresponse.IsSuccessStatusCode) return new Credentials("OAuth Generation Request Failed"); //Check if request is succesful
            var tempcreds = JsonConvert.DeserializeObject<AuthorizationCodeGrantFlow_Creds>(tokenresponse.Content.ReadAsStringAsync().Result);  //Deserialize data
            var valiresponse = ValidateToken(tempcreds.token_type, tempcreds.access_token); //Validate token using another request to twitch server
            if (!CheckValidationRequest(valiresponse)) return new Credentials("Token Validation Failed"); //Check validation response
            try
            {
                var creds = JsonConvert.DeserializeObject<Credentials>(valiresponse.Content.ReadAsStringAsync().Result); //Deserialize validation response
                creds.isSucces = true;
                creds.AuthToken = tempcreds.access_token;
                creds.RefreshToken = tempcreds.refresh_token;
                return creds;
            }
            catch { return new Credentials("Credential Deserialization Failed"); }
        }
        /// <summary>
        /// Checks the parameter returned by the browser
        /// </summary>
        private static bool AuthorizationCodeFlow_CheckParas(Dictionary<string, StringValues> paras, string state)
        {
            if (paras == null) return false;
            if (!paras.ContainsKey("state")) return false;
            if (paras["state"] != state) return false;
            if (paras.ContainsKey("error")) return false;
            if (!paras.ContainsKey("code") || paras["code"] == "") return false;
            return true;
        }
        private class AuthorizationCodeGrantFlow_Creds
        {
            public string access_token { get; set; }
            /// <summary>
            /// not provided when refreshing a token
            /// </summary>
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
            public List<string> scope { get; set; }
            public string token_type { get; set; }
        }
        #endregion
        #region ClientCredentialsGrantFlow
        /// <summary>
        /// The client credentials grant flow is meant only for server-to-server API requests.
        /// </summary>
        public static Credentials ClientCredentialsGrantFlow(string ClientId, string ClientSecret)
        {
            //Create request body - parameters can be found in the documentation
            var application = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("client_id", ClientId), new KeyValuePair<string, string>("client_secret", ClientSecret), new KeyValuePair<string, string>("grant_type", "client_credentials") };
            var tokenresponse = HeaderlessURLEncodedRequest("https://id.twitch.tv/oauth2/token", application, HttpMethod.Post); //Send request
            if (!tokenresponse.IsSuccessStatusCode) return new Credentials("OAuth Generation Request Failed"); //Check if request is succesful
            var tempcreds = JsonConvert.DeserializeObject<ClientCredentialsGrantFlow_Creds>(tokenresponse.Content.ReadAsStringAsync().Result); //Deserialize data
            var valiresponse = ValidateToken(tempcreds.token_type, tempcreds.access_token); //Validate token using another request to twitch server
            if (!CheckValidationRequest(valiresponse)) return new Credentials("Token Validation Failed"); //Check validation response
            try
            {
                var creds = JsonConvert.DeserializeObject<Credentials>(valiresponse.Content.ReadAsStringAsync().Result); //Deserialize validation response
                creds.isSucces = true;
                creds.AuthToken = tempcreds.access_token;
                return creds;
            }
            catch { return new Credentials("Credential Deserialization Failed"); }
        }
        private class ClientCredentialsGrantFlow_Creds
        {
            public string access_token { get; set; }
            public int expires_in { get; set; }
            public string token_type { get; set; }
        }
        #endregion
        #region TokenValidation
        /// <summary>
        /// Checks if a token is still valid using a request to the Twitch server
        /// </summary>
        public static bool IsTokenValid(string token, string tokentype)
        {
            var valiresponse = ValidateToken(tokentype, token); //Validate token using a request to twitch server
            return CheckValidationRequest(valiresponse); //Check validation response
        }
        #endregion
        #region Refresh Token
        /// <summary>
        /// Refreshes your acces token. Only works if AuthorizationCodeFlow was used to obtain your token.
        /// </summary>
        /// <param name="InCreds"></param>
        /// <param name="client_secret"></param>
        /// <returns></returns>
        public static Credentials RefreshToken(Credentials InCreds, string client_secret)
        {
            if (InCreds.RefreshToken == null) return new Credentials("Refresh Token is null or empty"); //Check if Refresh Token is provided. Can only be obtained by using AuthorizationCodeFlow.
            if (InCreds.RefreshToken == "") return new Credentials("Refresh Token is null or empty");
            var tokenresponse = HeaderlessURLEncodedRequest("https://id.twitch.tv/oauth2/token", new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("grant_type", "refresh_token"), new KeyValuePair<string, string>("refresh_token", InCreds.RefreshToken), new KeyValuePair<string, string>("client_id", InCreds.client_id), new KeyValuePair<string, string>("client_secret", client_secret) }, HttpMethod.Post);
            if (!tokenresponse.IsSuccessStatusCode) return new Credentials("OAuth Generation Request Failed"); //Check if request is succesful
            var tempcreds = JsonConvert.DeserializeObject<AuthorizationCodeGrantFlow_Creds>(tokenresponse.Content.ReadAsStringAsync().Result);  //Deserialize data
            var valiresponse = ValidateToken(tempcreds.token_type, tempcreds.access_token); //Validate token using another request to twitch server
            if (!CheckValidationRequest(valiresponse)) return new Credentials("Token Validation Failed"); //Check validation response
            try
            {
                var creds = JsonConvert.DeserializeObject<Credentials>(valiresponse.Content.ReadAsStringAsync().Result); //Deserialize validation response
                creds.isSucces = true;
                creds.AuthToken = tempcreds.access_token;
                creds.RefreshToken = tempcreds.refresh_token;
                return creds;
            }
            catch { return new Credentials("Credential Deserialization Failed"); }
        }
        #endregion
        #region important methods
        /// <summary>
        /// Creates a HttpListener and returns the redirected url
        /// </summary>
        /// <param name="redirecturl"></param>
        /// <returns>returns the full redirected url</returns>
        private static string Listener(string redirecturl)
        {
            if (redirecturl == null) return null;
            if (redirecturl.Length == 0) return null;
            if (redirecturl[redirecturl.Length - 1] != '/') //Check if last Char is '/', if not add '/' (listener won't work if the last char is not '/')
                redirecturl += '/';
            HttpListener server = new HttpListener(); //Create server
            server.Prefixes.Add(redirecturl); //Bind url to server
            server.Start(); //Start server
            do
            {
                HttpListenerContext context = server.GetContext(); //Get the request
                if (context.Request.Url.ToString() == redirecturl) //Thanks twitch, this is just pain.
                    //This next line sends a response with a script, to make another request with the URI fragment parsed to a proper "readable" url (URI fragments are usualy not part of a request, so we need another way to get to our information)
                    SendHttpResponse("<!DOCTYPE html>\r\n<html>\r\n<body onload=\"const Http = new XMLHttpRequest(); Http.open('GET','"+ redirecturl + "?' + window.location.hash.substring(1)); Http.send(); Http.onreadystatechange = function(){if(this.readyState==4 && this.status == 200) {window.close();}}\">\r\n</html>", context.Response); //there could be a more elegant way to do this
                else
                {
                    SendHttpResponse("<body onload=\"window.close();\">", context.Response); //Send response to close browser tab
                    return context.Request.Url.ToString(); //returns url with params
                }
            } while (true);
        }

        /// <summary>
        /// Sends a message to a HttpRequest
        /// </summary>
        private static void SendHttpResponse(string message, HttpListenerResponse response)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.Close();
        }
        /// <summary>
        /// Deserializes an URL
        /// </summary>
        /// <returns></returns>
        private static Dictionary<string, StringValues> GetUrlParams(string url)
        {
            string content;
            if (url.Contains("/?"))
                content = url.Split("/?")[1];
            else return null;
            using var reader = new FormReader(content);
            return reader.ReadForm();
        }
        /// <summary>
        /// Opens a browser with an url. (Can be simplified in non .net core apps with Process.Start("URL");)
        /// </summary>
        /// <param name="url"></param>
        private static void OpenBrowser(string url)
        {
            Process browser = new Process();
            browser.StartInfo.UseShellExecute = true;
            browser.StartInfo.FileName = url;
            browser.Start();
        }
        /// <summary>
        /// Checks the ValidateToken request.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static bool CheckValidationRequest(HttpResponseMessage message)
        {
            if (message == null) return false;
            if (!message.IsSuccessStatusCode) return false;
            try
            {
                var valifailed = JsonConvert.DeserializeObject<ValidationFailed>(message.Content.ReadAsStringAsync().Result);
                if (valifailed.status != 200 && valifailed.status != 0) return false;
            }
            catch { }
            return true;
        }
        /// <summary>
        /// Takes a list of scopes and returns a URL encoded string containing scopes.
        /// </summary>
        /// <param name="Scopes"></param>
        /// <returns></returns>
        private static string GenerateScopes(List<string> Scopes)
        {
            return new FormUrlEncodedContent(
                new List<KeyValuePair<string, string>>() 
                { 
                    new KeyValuePair<string, string>("scope", String.Join(", ", Scopes.ToArray())) 
                }).ReadAsStringAsync().Result;
        }
        #endregion
        #region HttpRequests
        /// <summary>
        /// HTTP request to the Twitch servers to check if a token is valid.
        /// </summary>
        /// <param name="tokentype"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        private static HttpResponseMessage ValidateToken(string tokentype, string token)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"{tokentype.FirstCharToUpper()} {token}");
                    var request = new HttpRequestMessage //Anfrage erstellen
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri("https://id.twitch.tv/oauth2/validate"),
                        Content = new StringContent("")
                    };
                    return client.SendAsync(request).Result; //Anfrage senden und Antwort abwarten
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); //Wenn fehler -> in Console ausgeben lassen
            }
            return null;
        }
        /// <summary>
        /// HTTP request to the Twitch servers. Used by AuthorizationCodeFlow, ClientCredentialsGrantFlow and RefreshToken to get tokens.
        /// </summary>
        /// <param name="url"></param>
        /// <param name="application"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        private static HttpResponseMessage HeaderlessURLEncodedRequest(string url, List<KeyValuePair<string, string>> application, HttpMethod method)
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    var request = new HttpRequestMessage //Anfrage erstellen
                    {
                        Method = method,
                        RequestUri = new Uri(url),
                        Content = new FormUrlEncodedContent(application)
                    };
                    return client.SendAsync(request).Result; //Anfrage senden und Antwort abwarten
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message); //Wenn fehler -> in Console ausgeben lassen
            }
            return null;
        }
        #endregion

        /// <summary>
        /// Returned by ImplicitGrantFlow, AuthorizationCodeFlow, ClientCredentialsGrantFlow and RefreshToken. Contains information about your credentials/token.
        /// </summary>
        public class Credentials
        {
            public bool isSucces = false;
            public string ErrorMessage = "";
            /// <summary>
            /// Generated User/App Acces Token, empty if request failed
            /// </summary>
            public string AuthToken = "";
            /// <summary>
            /// Only provided by Authorization code grant flow, empty if failed
            /// </summary>
            public string RefreshToken = "";
            public string client_id { get; set; }
            /// <summary>
            /// Only provided with an User Acces Token
            /// </summary>
            public string login { get; set; }
            /// <summary>
            /// Only provided with an User Acces Token
            /// </summary>
            public List<string> scopes { get; set; }
            /// <summary>
            /// Only provided with an User Acces Token
            /// </summary>
            public string user_id { get; set; }
            public int expires_in { get; set; }
            internal Credentials() { } //Constructor for Json converter
            internal Credentials(string ErrorMessage) { this.ErrorMessage = ErrorMessage; } //Constructor with errormessage

        }
        /// <summary>
        /// Used by CheckValidationRequest to determine if the token is valid
        /// </summary>
        private class ValidationFailed
        {
            public int status { get; set; }
            public string message { get; set; }
        }
    }
    public static class StringExtensions
    {
        public static string FirstCharToUpper(this string input) =>
            input switch
            {
                null => throw new ArgumentNullException(nameof(input)),
                "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
                _ => string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1))
            };
    }
}
