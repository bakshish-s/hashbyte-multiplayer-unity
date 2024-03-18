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
            await MultiplayerService.Instance.Initialize(playerId);
            DeactivateAllPanels();
            lobbyPanel.SetActive(true);
            MultiplayerService.Instance.JoinOrCreateGame();
            /**
            //Try to join a random game if available
            IRoomResponse roomResponse = await MultiplayerService.Instance.JoinOrCreateGameAsync(null);
            if (roomResponse.Success)
            {
                OnMultiplayerRoomJoined();
                if(roomResponse.Room != null)
                {
                    waitingMessage.text = "<color=green>Players in game\n";
                    for(int i=0; i<roomResponse.Room.Players.Count; i++)
                    {
                        waitingMessage.text += roomResponse.Room.Players[i]+"\n";
                    }                    
                }
            }
            */
        }

        public async void CreatePrivateGame()
        {
            MultiplayerService.Instance.RegisterCallbacks(this);
            //Initialize Multiplayer Services            
            await MultiplayerService.Instance.Initialize(playerId);
            lobbyPanel.SetActive(true);
            string roomCode = await MultiplayerService.Instance.CreatePrivateGameAsync(null);
            Debug.Log(roomCode);
        }

        public async void GUI_JoinAsyncGame(TMPro.TMP_InputField passCodeField)
        {
            MultiplayerService.Instance.RegisterCallbacks(this);
            //Initialize Multiplayer Services            
            await MultiplayerService.Instance.Initialize(playerId);
            DeactivateAllPanels();
            lobbyPanel.SetActive(true);
            IRoomResponse roomResponse = await MultiplayerService.Instance.JoinPrivateGameAsync(passCodeField.text);
            Debug.Log(roomResponse.Success);
        }

        private void DeactivateAllPanels()
        {
            menuPanel.SetActive(false);
            lobbyPanel.SetActive(false);
            waitingPanel.SetActive(false);
        }

        public void OnPlayerJoined(string playerName)
        {
            Debug.Log($"Player joined in MenuController");
            waitingMessage.text = "<color=red>Players in game\n";
            for (int i = 0; i < MultiplayerService.Instance.CurrentRoom.Players.Count; i++)
            {
                waitingMessage.text += MultiplayerService.Instance.CurrentRoom.Players[i] + "\n";
            }
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
                Debug.Log($"Host {room.isHost}, Lobby Code {room.LobbyCode}," +
                    $" Lobby Id {room.LobbyId}, Room Id {room.RoomId} ");
                waitingMessage.text += $"<b>{room.LobbyCode}\n{room.RoomId}";
                OnMultiplayerRoomJoined();
                foreach(INetworkPlayer player in room.players)
                {
                    Debug.Log($"Player in room {player.PlayerId}, {player.PlayerName}");
                }
                foreach(string key in room.RoomOptions.Keys)
                {
                    Debug.Log($"Options in room {key} -- {room.RoomOptions[key]}");
                }
            }
        }
    }
}