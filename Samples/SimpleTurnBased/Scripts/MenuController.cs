using Hashbyte.Multiplayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        HashbyteNetwork.Instance.OnInitialized += OnMultiplayerInitialized;
        HashbyteNetwork.Instance.OnGameJoined += OnMultiplayerGameJoined;
        if (!HashbyteNetwork.Instance.IsInitialized) HashbyteNetwork.Instance.Initialize(ServiceType.UNITY);
    }

    private void OnMultiplayerGameJoined(GameEvent gameEvent)
    {
        
    }

    private void OnMultiplayerInitialized()
    {
        HashbyteNetwork.Instance.OnInitialized -= OnMultiplayerInitialized;         //Unsubscribe as initialization happens only once during game launch
    }       

    public async void GUI_StartGame()
    {
        //Initialize Multiplayer Services
        if (!HashbyteNetwork.Instance.IsInitialized) await HashbyteNetwork.Instance.InitializeAsync(ServiceType.UNITY);
        //Try to join a random game if available
        await HashbyteNetwork.Instance.JoinRandomGameAsync();

    }

    private void Update()
    {
        HashbyteNetwork.Instance.Update();
    }
}
