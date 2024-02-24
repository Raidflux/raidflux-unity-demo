using System;
using System.Collections;
using System.Collections.Generic;
using MLAPI;
using MLAPI.SceneManagement;
using MLAPI.Transports.UNET;
using UnityEngine;
using UnityEngine.SceneManagement;
using Raidflux;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public NetworkManager networkManager;
    private UNetTransport unetTransport;

    public string deploymentID;
    public Text notFoundText;
    public Text gameserversNotFoundText;
    public MothershipButton mothershipButtonPrefab;
    public GameserverButton gameserverButtonPrefab;

    public RectTransform mainMenu;
    public RectTransform serverMenu;
    public RectTransform mothershipButtonContainer;
    public RectTransform serverButtonsContainer;
    
    private void Awake()
    {
        unetTransport = networkManager.GetComponentInChildren<UNetTransport>();
    }

    private void Start()
    {
       LoadMotherships();
    }

    public void LoadMotherships()
    {
        RaidfluxServer.Singleton.ListMotherships(deploymentID, list =>
        {
            if (list.Count == 0)
            {
                notFoundText.gameObject.SetActive(true);
            }
            else
            {
                notFoundText.gameObject.SetActive(false);
                while (mothershipButtonContainer.childCount > 0) {
                    DestroyImmediate(mothershipButtonContainer.GetChild(0).gameObject);
                }

                foreach (Mothership mothership in list)
                {
                    MothershipButton button = Instantiate(mothershipButtonPrefab, mothershipButtonContainer);
                    button.SetData(mothership, MothershipClicked);
                }
            }
        });
    }
    public void ConnectLocalHost()
    {
        SceneManager.LoadScene("MainScene");
        unetTransport.ConnectAddress = "127.0.0.1";
        unetTransport.ConnectPort = 7777;
        networkManager.StartClient();
    }

    public void StartAsHost()
    {
        networkManager.StartHost();
        NetworkSceneManager.SwitchScene("MainScene");

    }

    private void MothershipClicked(Mothership mothership)
    {
        while (serverButtonsContainer.childCount > 0) {
            DestroyImmediate(serverButtonsContainer.GetChild(0).gameObject);
        }
        serverMenu.gameObject.SetActive(true);
        mainMenu.gameObject.SetActive(false);
        
        RaidfluxServer.Singleton.ListGameservers(mothership.id, list =>
        {
            if (list.Count == 0)
            {
                gameserversNotFoundText.gameObject.SetActive(true);
            }
            else
            {
                gameserversNotFoundText.gameObject.SetActive(false);
                foreach (Gameserver gameserver in list)
                {
                    GameserverButton button = Instantiate(gameserverButtonPrefab, serverButtonsContainer);
                    button.SetData(gameserver, GameserverClicked);
                }

                Util.RefreshContentFitter(serverButtonsContainer.GetComponent<RectTransform>());
            }

        });
    }

    private void GameserverClicked(Gameserver gameserver)
    {
        int port = 0;
        foreach (Port gameserverPort in gameserver.ports)
        {
            if (gameserverPort.internalPort == unetTransport.ServerListenPort)
            {
                port = gameserverPort.port;
                break;
            }
        }

        if (port == 0)
        {
            Debug.LogError("No UDP port found for gameserver, make sure you have correctly set up your gameserver.ini");
            serverMenu.gameObject.SetActive(false);
            mainMenu.gameObject.SetActive(true);
            LoadMotherships();
        }
        else
        {
            SceneManager.LoadScene("MainScene");
            unetTransport.ConnectAddress = gameserver.ipv4;
            unetTransport.ConnectPort = port;
            networkManager.StartClient();
        }
    }
}
