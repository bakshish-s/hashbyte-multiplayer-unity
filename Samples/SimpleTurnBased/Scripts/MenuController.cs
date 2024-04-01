using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
namespace Hashbyte.Multiplayer.Demo
{
    public class MenuController : MonoBehaviour, INetworkEvents
    {
        public string playerId;
        public GameObject menuPanel, lobbyPanel, waitingPanel;
        public UnityEngine.UI.Button playButton;
        public TMPro.TextMeshProUGUI waitingMessage;
        public TMPro.TextMeshProUGUI pingMessage;
        public UnityEngine.UI.Image networkIndicator;
        public bool isConnected;
        public TextMeshProUGUI messageQueue;
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
        public void GUI_LeaveGame()
        {
            MultiplayerService.Instance.LeaveRoom();
            DeactivateAllPanels(menuPanel);
        }

        public void GUI_FindRooms()
        {
            MultiplayerService.Instance.FindAvailableRooms();
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
            waitingMessage.text += $"<b>({player.ActorNumber}) {player.PlayerName}\n";
        }

        void INetworkEvents.OnPlayerConnected()
        {
            playButton.interactable = true;
            MultiplayerService.Instance.SendMove(new GameEvent() { eventType = GameEventType.GAME_STARTED });
        }
        int ping = 0; int pong = 0; int timeer = 0;
        public void OnNetworkMessage(GameEvent gameEvent)
        {
            switch (gameEvent.eventType)
            {
                case GameEventType.GAME_STARTED:
                    break;
                case GameEventType.GAME_ENDED:
                    break;
                case GameEventType.GAME_MOVE:
                    UpdateMessageQueue(gameEvent.data);
                    break;
                case GameEventType.END_TURN:
                    break;
                case GameEventType.GAME_ALIVE:
                    timeer--;
                    pingMessage.text = $"Timer {timeer}";
                    break;
                case GameEventType.PING:
                    ping++;                    
                    break;
                case GameEventType.PONG:
                    pong++;
                    break;
                case GameEventType.PLAYER_RECONNECTED:
                    break;
                default:
                    break;
            }
                    pingMessage.text = $"Ping/Pong {ping}/{pong}";
        }

        public void GUI_SendMessage(TMP_InputField messageField)
        {
            string message = messageField.text;
            if (string.IsNullOrEmpty(message)) message = "Ping";
            MultiplayerService.Instance.SendMove(new GameEvent() { eventType = GameEventType.GAME_MOVE, data = message });
        }

        private void UpdateMessageQueue(string message)
        {
            messageQueue.text += $"\n{message}";
        }

        public void OnRoomPropertiesUpdated(Hashtable roomProperties)
        {
            Debug.Log("Room properties updated");

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
            if (room != null)
            {
                waitingMessage.text = "Room Joined\n";
                //Debug.Log($"Host {room.isHost}, Lobby Code {room.LobbyCode}," +
                //    $" Lobby Id {room.LobbyId}, Room Id {room.RoomId} ");
                waitingMessage.text += $"<b>{room.LobbyCode}--{room.RoomId}\n{room.RoomOptions["seed"]}\n";
                OnMultiplayerRoomJoined();
                foreach (string key in room.RoomOptions.Keys)
                {
                    Debug.Log($"Options in room {key} -- {room.RoomOptions[key]}");
                }
                if (room.isPrivateRoom)
                {
                    waitingMessage.text += $"Room Code\n<size=200%>{room.RoomCode}</size>\n";
                }
                foreach (int actorNumber in room.players.Keys)
                {
                    //Debug.Log($"Player in room {player.PlayerId}, {player.PlayerName}");
                    waitingMessage.text += $"<b>({actorNumber}) {room.players[actorNumber].PlayerName}\n";
                }
            }
        }

        private void OnMultiplayerRoomJoined()
        {
            DeactivateAllPanels(waitingPanel);
        }

        public void OnPlayerLeft(INetworkPlayer player)
        {
            waitingMessage.text += $"<color=red>({player.ActorNumber}) {player.PlayerName}</color>\n";
        }

        public void OnRoomDeleted()
        {
            StartCoroutine(ShowRoomDeleted());
        }

        IEnumerator ShowRoomDeleted()
        {
            waitingMessage.text = "Host deleted room. Closing Game";
            yield return new WaitForSeconds(2);
            DeactivateAllPanels(menuPanel);
        }

        void INetworkEvents.OnPlayerDisconnected()
        {
            waitingMessage.text += "<color=orange>Other Player Disconnected</color>\n";
        }

        public void OnPlayerReconnected()
        {
            waitingMessage.text += "<color=green>Other Player Reconnected</color>\n";
        }

        public void OnConnectionStatusChange(bool connected)
        {
            if (connected)
            {
                networkIndicator.color = Color.green;
                waitingMessage.text += "<color=green>INTERNET ON</color>\n";
            }
            else
            {
                networkIndicator.color = Color.red;
                waitingMessage.text += "<color=red>INTERNET OFF</color>\n";
            }
        }
    }
}