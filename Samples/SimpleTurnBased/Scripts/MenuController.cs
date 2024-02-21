using Hashbyte.Multiplayer;
using System.Collections;
using UnityEngine;
namespace Hashbyte.Multiplayer.Demo
{
    public class MenuController : MonoBehaviour, INetworkEvents
    {
        public string playerId;
        public GameObject menuPanel, lobbyPanel, waitingPanel;
        public UnityEngine.UI.Button playButton;
        public TMPro.TextMeshProUGUI waitingMessage;

        private void OnMultiplayerRoomJoined()
        {
            DeactivateAllPanels();
            waitingPanel.SetActive(true);
        }

        public async void GUI_StartGame()
        {
            MultiplayerService.Instance.RegisterCallbacks(this);
            //Initialize Multiplayer Services            
            await MultiplayerService.Instance.Initialize(new NetworkPlayer() { PlayerId = playerId });
            DeactivateAllPanels();
            lobbyPanel.SetActive(true);
            Hashtable roomOptions = new Hashtable
            {
                { "Host", playerId }
            };
            //Try to join a random game if available
            IRoomResponse roomResponse = await MultiplayerService.Instance.JoinOrCreateGame(roomOptions);
            if (roomResponse.Success)
            {
                OnMultiplayerRoomJoined();
                if(roomResponse.RoomOptions != null)
                {
                    if (roomResponse.RoomOptions.ContainsKey("Host"))
                    {
                        waitingMessage.text = roomResponse.RoomOptions["Host"].ToString();
                    }
                }
            }

        }

        private void DeactivateAllPanels()
        {
            menuPanel.SetActive(false);
            lobbyPanel.SetActive(false);
            waitingPanel.SetActive(false);
        }

        public void OnPlayerJoined(/*int actorNumber, string playerId, string playerName*/)
        {
            waitingMessage.text = $"Player Joined";
        }

        void INetworkEvents.OnPlayerConnected()
        {
            //UnityEngine.SceneManagement.SceneManager.LoadScene(1);
        }

        public void OnNetworkMessage(GameEvent gameEvent)
        { }

        private void Update()
        {
            if(Input.GetKeyDown(KeyCode.U))
            {
                Hashtable roomProperties = new Hashtable
                {
                    { "seed", 1234 }
                };
                MultiplayerService.Instance.UpdateRoomProperties(roomProperties);
            }
        }

        public void OnRoomPropertiesUpdated(Hashtable roomProperties)
        {
            Debug.Log("Room properties updated");
        }
    }
}