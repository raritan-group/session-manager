using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Xml;

namespace Clean_Up_P21_Sessions
{
    internal class Program
    {       
        private HttpClient client;
        private String baseUri;
        private String cookie;
        private String authToken;
        private XmlDocument xmlDocument;
        private string aspDotNetSessionId;
        private string userName;
        private string password;
        private string playBaseUri;
        public Program ()
        {
            //enter your live uri here

            Console.WriteLine("Enter your middleware URI: ");
            baseUri = Console.ReadLine();
            //potential other middleware instances
            //playBaseUri = "";
            aspDotNetSessionId = " ASP.NET_SessionId=qbeh5xivfjs3hjpg5ddpxodg";
            client = new HttpClient();
            client.BaseAddress = new Uri("https://" + baseUri);
            xmlDocument = new XmlDocument();
            authTokenRequest();
            cookie = " soaCookie=" + authToken + ";" + aspDotNetSessionId;
        }

        public void authTokenRequest() 
        {
            Console.Write("Please enter your user name:");
            userName = Console.ReadLine();
            Console.Write("Please enter your password:");
            password = Console.ReadLine();
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://" + baseUri + "/api/security/token/");
                request.Headers.Add("Host", baseUri);
                request.Headers.Add("User-Agent", " Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/118.0");
                request.Headers.Add("Accept", " application/xml");
                request.Headers.Add("Accept-Language", " en-US,en;q=0.5");
                request.Headers.Add("Accept-Encoding", " gzip, deflate, br");
                request.Headers.Add("username", userName);
                request.Headers.Add("password", password);
                request.Headers.Add("X-Requested-With", " XMLHttpRequest");
                request.Headers.Add("Origin", baseUri);
                request.Headers.Add("Connection", " keep-alive");
                request.Headers.Add("Referer", "https://" + baseUri + "/docs/logon.aspx");
                request.Headers.Add("Cookie", aspDotNetSessionId);
                request.Headers.Add("Sec-Fetch-Dest", " empty");
                request.Headers.Add("Sec-Fetch-Mode", " cors");
                request.Headers.Add("Sec-Fetch-Site", " same-origin");
                request.Headers.Add("Pragma", " no-cache");
                request.Headers.Add("Cache-Control", " no-cache");
                //request.Headers.Add("Content-Length", " 0");
                request.Headers.Add("TE", " trailers");
                var response = this.client.SendAsync(request);
                response.Wait();
                response.Result.EnsureSuccessStatusCode();
                xmlDocument.LoadXml(response.Result.Content.ReadAsStringAsync().Result);           
                authToken = "Bearer " + xmlDocument.DocumentElement.FirstChild.InnerText;
               
            }
            catch (Exception ex) 
            {                           
                Console.WriteLine("Unable to retreive token.\n" + ex);
                if (ex.Message.Contains("Unauthorized"))
                {
                    Console.WriteLine("Unable to authenticate please renter your information.");
                    Console.WriteLine("Incorrect username or password.");
                    this.authTokenRequest();
                }               
            }
        }
        public void LogOutDuplicateSessions(JsonNode session)
        {
            var licenseId = session["SessionId"];
            var request2 = new HttpRequestMessage(HttpMethod.Delete, "/api/licensing/usage/" + licenseId);
            request2.Headers.Add("Host", "p21.raritanpipe.com");
            request2.Headers.Add("User-Agent", " Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/118.0");
            request2.Headers.Add("Accept", " application/json");
            request2.Headers.Add("Accept-Language", " en-US,en;q=0.5");
            request2.Headers.Add("Accept-Encoding", " gzip, deflate, br");
            request2.Headers.Add("Authorization", authToken);
            request2.Headers.Add("X-Requested-With", " XMLHttpRequest");
            request2.Headers.Add("Origin", " https://p21.raritanpipe.com");
            request2.Headers.Add("Connection", " keep-alive");
            request2.Headers.Add("Referer", " https://p21.raritanpipe.com/UiServer/");
            request2.Headers.Add("Cookie", cookie);
            request2.Headers.Add("Sec-Fetch-Dest", " empty");
            request2.Headers.Add("Sec-Fetch-Mode", " cors");
            request2.Headers.Add("Sec-Fetch-Site", " same-origin");
            request2.Headers.Add("TE", " trailers");
            var response2 = client.SendAsync(request2);
            response2.Wait();
            response2.Result.EnsureSuccessStatusCode();
        }
                
        public JsonArray getUserSessions()
        {
            try
            {
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "/api/licensing/usage");
                request.Headers.Add("Host", "p21.raritanpipe.com");
                request.Headers.Add("User-Agent", " Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/118.0");
                request.Headers.Add("Accept", " application/json");
                request.Headers.Add("Accept-Language", " en-US,en;q=0.5");
                request.Headers.Add("Accept-Encoding", " gzip, deflate, br");
                request.Headers.Add("Authorization", authToken);
                request.Headers.Add("X-Requested-With", " XMLHttpRequest");
                request.Headers.Add("Connection", " keep-alive");
                request.Headers.Add("Referer", " https://" + baseUri + "/UiServer/");
                request.Headers.Add("Cookie", cookie);
                request.Headers.Add("Sec-Fetch-Dest", " empty");
                request.Headers.Add("Sec-Fetch-Mode", " cors");
                request.Headers.Add("Sec-Fetch-Site", " same-origin");
                Task<HttpResponseMessage> response = client.SendAsync(request);
                response.Wait();
                response.Result.EnsureSuccessStatusCode();
                string json = response.Result.Content.ReadAsStringAsync().Result;
                JsonNode jsonNode = JsonObject.Parse(json);
                var userSessions = jsonNode["LicenseUsageList"].AsArray();
                return userSessions;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }            
        }
        public void deleteDuplicateSessions(JsonArray userSessions)
        {                      
            try
            {                             
                var sortedSessions = userSessions.OrderBy(e => DateTime.Parse(e["AcquisitionTime"].ToString())).Reverse();
                HashSet<JsonNode> uniqueSessions = new HashSet<JsonNode>();
                var duplicateSessions = new List<JsonNode>();
                bool isDuplicate;
                foreach (var session1 in sortedSessions)
                {
                   isDuplicate = false;
                   foreach(var session2 in uniqueSessions) 
                   {
                        if (session1["UserName"].GetValue<String>().Equals(session2["UserName"].GetValue<String>()))
                        {
                            duplicateSessions.Add(session1);
                            isDuplicate = true;
                            break;
                        }                        
                   }
                    if (!isDuplicate)
                    { 
                        uniqueSessions.Add(session1);
                    } 
                }
                duplicateSessions.ForEach(LogOutDuplicateSessions);                           
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(HttpRequestException))
                    Console.WriteLine(ex.ToString() + "\nMake sure to check that the originating middleware session is still alive");
            }                  
        }  

        public void KillProgramSession()
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://" + baseUri + "/docs/logon.aspx");
                request.Headers.Add("Host", " p21.raritanpipe.com");
                request.Headers.Add("User-Agent", " Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/118.0");
                request.Headers.Add("Accept", " text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
                request.Headers.Add("Accept-Language", " en-US,en;q=0.5");
                request.Headers.Add("Accept-Encoding", " gzip, deflate, br");
                request.Headers.Add("Connection", " keep-alive");
                request.Headers.Add("Referer", " https://" + baseUri + "/UiServer/");
                request.Headers.Add("Cookie", cookie);
                request.Headers.Add("Upgrade-Insecure-Requests", " 1");
                request.Headers.Add("Sec-Fetch-Dest", " document");
                request.Headers.Add("Sec-Fetch-Mode", " navigate");
                request.Headers.Add("Sec-Fetch-Site", " same-origin");
                request.Headers.Add("Sec-Fetch-User", " ?1");
                request.Headers.Add("Pragma", " no-cache");
                request.Headers.Add("Cache-Control", " no-cache");
                var response = client.SendAsync(request);
                response.Result.EnsureSuccessStatusCode();
                Console.WriteLine(response.Result.Content.ReadAsStringAsync().Result);
            }
            catch (Exception ex) 
            {
                Console.WriteLine(ex.Message);
            }

        }

        public void KillAllUserSessions() 
        {
            var userSessions = this.getUserSessions();
            foreach(var userSession in userSessions) 
            {
                var licenseId = userSession["SessionId"];
                var request2 = new HttpRequestMessage(HttpMethod.Delete, "/api/licensing/usage/" + licenseId);
                request2.Headers.Add("Host", "p21.raritanpipe.com");
                request2.Headers.Add("User-Agent", " Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/118.0");
                request2.Headers.Add("Accept", " application/json");
                request2.Headers.Add("Accept-Language", " en-US,en;q=0.5");
                request2.Headers.Add("Accept-Encoding", " gzip, deflate, br");
                request2.Headers.Add("Authorization", authToken);
                request2.Headers.Add("X-Requested-With", " XMLHttpRequest");
                request2.Headers.Add("Origin", " https://" + baseUri);
                request2.Headers.Add("Connection", " keep-alive");
                request2.Headers.Add("Referer", " https://" + baseUri + "/UiServer/");
                request2.Headers.Add("Cookie", cookie);
                request2.Headers.Add("Sec-Fetch-Dest", " empty");
                request2.Headers.Add("Sec-Fetch-Mode", " cors");
                request2.Headers.Add("Sec-Fetch-Site", " same-origin");
                request2.Headers.Add("TE", " trailers");
                var response2 = client.SendAsync(request2);
                response2.Wait();
                response2.Result.EnsureSuccessStatusCode();
            }

        }
        static void Main(string[] args)
        {
            var program = new Program();            
            Boolean infinite = true;            
            
            Console.WriteLine("Welcome to Paul's P21 session deleter.\nDuplicate sessions are deleted every 30 seconds. The oldest session is chosen as the deleted session. All sessions are deleted at midnight everynight.");
            Reader reader = new Reader();
            while (infinite)
            {
                
                Console.WriteLine("What would you like to do? Options are:\ndelete all sessions\nexit");
                Console.WriteLine("Press enter to force clean up duplicate sessions.");
                try
                {
                    string command = Reader.ReadLine(30000);
                    if (command == "exit")
                    {
                        infinite = false;
                        program.KillProgramSession();
                    }
                    else if (command == "delete all sessions")
                    {
                        program.KillAllUserSessions();
                    }
                    else 
                    { 
                        //Do nothing
                    }
                                                    
                   
                } catch (Exception ex) 
                {
                    
                }
                Console.WriteLine(DateTime.Now + ": Cleaning sessions up...");
                Console.WriteLine("Getting sessions");
                var userSessions = program.getUserSessions();
                Console.WriteLine("Session retreived.");
                Console.WriteLine("Identifying duplicate sessions and deleteing");
                program.deleteDuplicateSessions(userSessions);                  
                Console.WriteLine(DateTime.Now + ": Sessions cleaned");
                Console.WriteLine(DateTime.Now + ": Starting from beginning...");
                var dateTime = new DateTime();
                if (DateTime.Now.Hour == dateTime.Hour && DateTime.Now.Minute == dateTime.Minute)
                {
                    Console.WriteLine(DateTime.Now + "It is now midnight\nDeleting all sessions...");
                    program.KillAllUserSessions();
                    Console.WriteLine(DateTime.Now + "Deleted all sessions.");
                }
            }                     
        }
    }
}
