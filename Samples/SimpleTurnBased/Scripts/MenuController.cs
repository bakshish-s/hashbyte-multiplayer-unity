using Hashbyte.Multiplayer;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuPanel, lobbyPanel, waitingPanel;
    // Start is called before the first frame update
    void Start()
    {                
        HashbyteNetwork.Instance.OnJoinedRoom += OnMultiplayerRoomJoined;
    }

    private void OnMultiplayerRoomJoined()
    {        
        DeactivateAllPanels();
        waitingPanel.SetActive(true);
    }

    public async void GUI_StartGame()
    {
        //Initialize Multiplayer Services
        if (!HashbyteNetwork.Instance.IsInitialized) await HashbyteNetwork.Instance.InitializeAsync(ServiceType.UNITY);
        DeactivateAllPanels();
        lobbyPanel.SetActive(true);
        //Try to join a random game if available
        await HashbyteNetwork.Instance.JoinRandomGameAsync();

    }

    private void DeactivateAllPanels()
    {
        menuPanel.SetActive(false);
        lobbyPanel.SetActive(false);
        waitingPanel.SetActive(false);
    }
}
