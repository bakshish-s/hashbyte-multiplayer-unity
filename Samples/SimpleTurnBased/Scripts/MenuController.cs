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

        private Hashtable seedOption;

        async void Start()
        {
            DeactivateAllPanels(null);
            seedOption = new Hashtable { { "seed", System.Guid.NewGuid() } };
            MultiplayerService.Instance.RegisterCallbacks(this);
            await MultiplayerService.Instance.Initialize(playerId);
            menuPanel.SetActive(true);
        }        
        public void GUI_StartGame()
        {                   
            DeactivateAllPanels(lobbyPanel);            
            MultiplayerService.Instance.JoinOrCreateGame(seedOption);            
        }

        public void CreatePrivateGame()
        {                        
            DeactivateAllPanels(lobbyPanel);
            MultiplayerService.Instance.CreatePrivateGame(seedOption);            
        }

        public void GUI_JoinAsyncGame(TMPro.TMP_InputField passCodeField)
        {                        
            DeactivateAllPanels(lobbyPanel);
            MultiplayerService.Instance.JoinPrivateGame(passCodeField.text);            
        }

        private void DeactivateAllPanels(GameObject panelToShow)
        {
            menuPanel.SetActive(false);
            lobbyPanel.SetActive(false);
            waitingPanel.SetActive(false);
            panelToShow?.SetActive(true);
        }

        public void OnPlayerJoined(INetworkPlayer player)
        {
            Debug.Log($"Player joined in MenuController");
            waitingMessage.text += $"<b>{player.PlayerName}\n";            
        }

        void INetworkEvents.OnPlayerConnected()
        {
            playButton.interactable = true;
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
            //Debug.Log("Room properties updated");
            //waitingMessage.text = "Players in game\n";
            //if (MultiplayerService.Instance.CurrentRoom == null) return;
            //for (int i = 0; i < MultiplayerService.Instance.CurrentRoom.Players.Count; i++)
            //{
            //    waitingMessage.text += MultiplayerService.Instance.CurrentRoom.Players[i] + "\n";
            //}
        }

        public void OnRoomJoined(GameRoom room)
        {
            Debug.Log($"Room joined succesfully");
            if(room != null)
            {
                waitingMessage.text = "Room Joined\n";
                //Debug.Log($"Host {room.isHost}, Lobby Code {room.LobbyCode}," +
                //    $" Lobby Id {room.LobbyId}, Room Id {room.RoomId} ");
                waitingMessage.text += $"<b>{room.LobbyCode}--{room.RoomId}\n{room.RoomOptions["seed"]}\n";
                OnMultiplayerRoomJoined();
                foreach(string key in room.RoomOptions.Keys)
                {
                    Debug.Log($"Options in room {key} -- {room.RoomOptions[key]}");
                }
                if (room.isPrivateRoom)
                {
                    waitingMessage.text += $"Room Code\n<size=200%>{room.RoomCode}</size>\n";
                }
                foreach(INetworkPlayer player in room.players)
                {
                    //Debug.Log($"Player in room {player.PlayerId}, {player.PlayerName}");
                    waitingMessage.text += $"<b>{player.PlayerName}\n";
                }
            }
        }

        private void OnMultiplayerRoomJoined()
        {
            DeactivateAllPanels(waitingPanel);
        }        
    }
}