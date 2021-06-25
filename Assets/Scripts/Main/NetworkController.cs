using MLAPI;
using MLAPI.Transports.UNET;
using UnityEngine;
using Raidflux;

[RequireComponent(typeof(NetworkManager))]
public class NetworkController : MonoBehaviour
{
    private NetworkManager networkManager;
    private UNetTransport uNetTransport;
    private void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        uNetTransport = GetComponent<UNetTransport>();

        networkManager.OnServerStarted += () =>
        {
            if (networkManager.IsServer)
            {
                RaidfluxServer.Singleton.Init(uNetTransport.MaxConnections);
                
                networkManager.OnClientConnectedCallback += UpdatePlayerCount;
                networkManager.OnClientDisconnectCallback += UpdatePlayerCount;
            }
        };
    }

    private void UpdatePlayerCount(ulong id)
    {
        RaidfluxServer.Singleton.ReportPlayerCount(
            networkManager.ConnectedClients.Count, 
            uNetTransport.MaxConnections
        );
    }
    
}
