using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MLAPI;
using UnityEngine;

public class NoNetSpawner : MonoBehaviour
{
    public NetworkManager networkManagerPrefab;
    // Start is called before the first frame update
    void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            NetworkManager networkManager = Instantiate(networkManagerPrefab);
            networkManager.StartHost();
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
