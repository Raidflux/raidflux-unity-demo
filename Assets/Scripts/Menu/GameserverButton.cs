using System;
using UnityEngine;
using Raidflux;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class GameserverButton : MonoBehaviour
{
    private Gameserver gameserver;
    private Action<Gameserver> onClicked;
    private Button button;
    public Text buttonText;
    public Text playerInfoText;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        button.onClick.AddListener(() =>
        {
            this.onClicked.Invoke(this.gameserver);
        });
    }

    public void SetData(Gameserver gameserver, Action<Gameserver> onClicked)
    {
        this.gameserver = gameserver;
        this.onClicked = onClicked;

        int port = 0;
        foreach (Port gameserverPort in gameserver.ports)
        {
            if (gameserverPort.protocol == "udp")
            {
                port = gameserverPort.port;
                break;
            }
        }

        if (port == 0)
        {
            foreach (Port gameserverPort in gameserver.ports)
            {
                if (gameserverPort.protocol == "tcp")
                {
                    port = gameserverPort.port;
                    break;
                }
            }
        }
        
        buttonText.text = $"{gameserver.ipv4}:{port.ToString()}";
        playerInfoText.text = $"Players: {gameserver.playerCount}/{gameserver.maxPlayerCount}";
        Util.RefreshContentFitter(GetComponent<RectTransform>());
    }
}