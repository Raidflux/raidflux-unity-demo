using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace Raidflux
{
    public class Mothership
    {
        public string id;
        public string region;
        public double distance;
        public double latitude;
        public double longitude;
    }
    
    public class Gameserver
    {
        public int id;
        public int playerCount;
        public int maxPlayerCount;
        public string ipv4;
        public string ipv6;
        public List<Port> ports;
    }

    public class Port
    {
        public int port;
        public int internalPort;
        public string protocol;
    }
    
    public class RaidfluxServer: MonoBehaviour
    {
        private bool initialized = false;
        private UInt64 gameserverID = 0;
        private int currentPlayers = 0;
        private int maxPlayers = 20;

        private const string REGISTER_API = "/rfsdk/gameserver/register";
        private const string REPORT_API = "/rfsdk/gameserver/report";
        private const string MATCHMAKER_HOST = "https://matchmaker.raidflux.com";
        private const string MATCHMAKER_MOTHERSHIPS = "/discovery/deployment/{0}/motherships";
        private const string MATCHMAKER_GAMSERVERS = "/discovery/mothership/{0}/gameservers";

        public static RaidfluxServer Singleton = null;
        
        private class RegisterGameserverRequest
        {
            public string seat_id;
            public UInt64 max_player_count;
        }
        
        private class ReportPlayerCountRequest
        {
            public UInt64 gameserver_id;
            public UInt64 player_count;
            public UInt64 max_player_count;
        }
        
        private class RegisterGameserverResponse
        {
            public UInt64 gameserver_id;
        }
        
        private static string GetSDKHost()
        {
            return Environment.GetEnvironmentVariable("RAIDFLUX_SDK_HOST") ?? "localhost:8010";
        }
        
        private static string GetSeatID()
        {
            return Environment.GetEnvironmentVariable("RAIDFLUX_SEAT_ID") ?? "null";
        }

        private static string MakeSDKUrl(string path)
        {
            return "http://" + GetSDKHost() + path;
        }
        
        private static string MakeMatchmakerUrl(string path, string id)
        {
            return $"{MATCHMAKER_HOST}{String.Format(path, id)}";
        }

        private void Awake()
        {
            if (Singleton == null)
            {
                Singleton = this;
            }
            else
            {
                Destroy(this);
                Debug.LogError("You can only have one instance of RaidfluxServer at the time");
            }
        }

        private void Start()
        {
            if (transform.root == transform || transform.root == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Singleton == this)
            {
                Singleton = null;
            }
        }

        public void Init(int maxPlayers)
        {
            if(initialized) return;
            
            StartCoroutine(InitSdk(maxPlayers));
        }

        public void ReportPlayerCount(int currentPlayers, int maxPlayers)
        {
            if ((this.currentPlayers == currentPlayers && this.maxPlayers == maxPlayers) || GetSeatID() == "null") return;

            if (currentPlayers < 0)
            {
                currentPlayers = 0;
            }

            if (maxPlayers < 0)
            {
                maxPlayers = 0;
            }

            this.maxPlayers = maxPlayers;
            this.currentPlayers = currentPlayers;

            StartCoroutine(ReportPlayerCountSdk(currentPlayers, maxPlayers));
        }


        public void ListMotherships(string deploymentID, Action<List<Mothership>> onComplete)
        {
            StartCoroutine(ListMothershipsSdk(deploymentID, onComplete));
        }
        
        public void ListGameservers(string mothershipID, Action<List<Gameserver>> onComplete)
        {
            StartCoroutine(ListGameserversSdk(mothershipID, onComplete));
        }

        private IEnumerator InitSdk(int maxPlayers)
        {
            RegisterGameserverRequest request = new RegisterGameserverRequest
            {
                seat_id = GetSeatID(),
                max_player_count = Convert.ToUInt64(maxPlayers),
            };

            string jsonString = JsonConvert.SerializeObject(request);
            
            var uwr = new UnityWebRequest(MakeSDKUrl(REGISTER_API), "POST");
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonString);
            uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success){
                RegisterGameserverResponse response = JsonConvert.DeserializeObject<RegisterGameserverResponse>(uwr.downloadHandler.text);
                gameserverID = response.gameserver_id;
                initialized = true;
            }

            Debug.Log("Raidflux: Initialized");
        }

        private IEnumerator ReportPlayerCountSdk(int currentPlayers, int maxPlayers)
        {
            while (!this.initialized)
            {
                yield return null;
            }
            
            ReportPlayerCountRequest request = new ReportPlayerCountRequest
            {
                gameserver_id = Convert.ToUInt64(gameserverID),
                player_count = Convert.ToUInt64(currentPlayers),
                max_player_count = Convert.ToUInt64(maxPlayers)
            };

            string jsonString = JsonConvert.SerializeObject(request);
            
            var uwr = new UnityWebRequest(MakeSDKUrl(REPORT_API), "POST");
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonString);
            uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            yield return uwr.SendWebRequest();
            
            Debug.Log("Raidflux: PlayerCount reported");
        }

        private IEnumerator ListMothershipsSdk(string id, Action<List<Mothership>> onComplete)
        {
            var uwr = new UnityWebRequest(MakeMatchmakerUrl(MATCHMAKER_MOTHERSHIPS, id), "GET");
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Accept", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                List<Mothership> motherships = new List<Mothership>();
                var mothershipsJson = JArray.Parse(uwr.downloadHandler.text);
                foreach (var jToken in mothershipsJson)
                {
                    var mothershipJson = (JObject) jToken;
                    if (mothershipJson.ContainsKey("id") && mothershipJson.ContainsKey("vm"))
                    {
                        var vmJson = mothershipJson.Value<JObject>("vm")?.Value<JObject>("zone")
                            ?.Value<JObject>("region");

                        if (vmJson != null)
                        {
                            motherships.Add(new Mothership
                            {
                                id = mothershipJson.Value<string>("id"),
                                distance = mothershipJson.Value<double>("distance_to_user"),
                                region = vmJson.Value<string>("name"),
                                latitude = vmJson.Value<double>("latitude"),
                                longitude = vmJson.Value<double>("longitude")
                            });
                        }
                    }
                }

                onComplete(motherships);
            }

        }
        
        private IEnumerator ListGameserversSdk(string id, Action<List<Gameserver>> onComplete)
        {
            var uwr = new UnityWebRequest(MakeMatchmakerUrl(MATCHMAKER_GAMSERVERS, id), "GET");
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Accept", "application/json");

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                List<Gameserver> gameservers = new List<Gameserver>();
                var gameserversJson = JArray.Parse(uwr.downloadHandler.text);
                foreach (var jToken in gameserversJson)
                {
                    var gameserverJson = (JObject) jToken;
                    if (gameserverJson.ContainsKey("id") && gameserverJson.ContainsKey("fleetship_instance") && gameserverJson.ContainsKey("ports"))
                    {
                        List<Port> ports = new List<Port>();
                        foreach (var portJToken in gameserverJson.Value<JArray>("ports"))
                        {
                            var portJson = (JObject) portJToken;
                            ports.Add(new Port
                            {
                                port = portJson.Value<int>("port"),
                                internalPort = portJson.Value<int>("internal_port"),
                                protocol = portJson.Value<string>("protocol")
                            });
                            
                        }

                        gameservers.Add(new Gameserver
                        {
                            id = gameserverJson.Value<int>("id"),
                            playerCount = gameserverJson.Value<int>("current_player_count"),
                            maxPlayerCount = gameserverJson.Value<int>("max_player_count"),
                            ipv4 = gameserverJson.Value<JObject>("fleetship_instance")?.Value<string>("ipv4") ?? "",
                            ipv6 = gameserverJson.Value<JObject>("fleetship_instance")?.Value<string>("ipv6") ?? "",
                            ports = ports
                        });
                    }
                }

                onComplete(gameservers);
            }

        }
    }

}
