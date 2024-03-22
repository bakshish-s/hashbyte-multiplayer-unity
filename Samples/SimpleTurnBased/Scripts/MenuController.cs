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
        public UnityEngine.UI.Image networkIndicator;

        private Hashtable seedOption;
#if UNITY_ANDROID
        AndroidJavaObject checkNetwork;
#endif
        async void Start()
        {            
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");            
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");            
            checkNetwork = new AndroidJavaObject("com.hashbytestudio.checknetwork.CheckNetwork", currentActivity);
            HearbeatNetwork();
            DeactivateAllPanels(null);
            seedOption = new Hashtable { { "seed", System.Guid.NewGuid() } };
            MultiplayerService.Instance.RegisterCallbacks(this);
            await MultiplayerService.Instance.Initialize(playerId);
            menuPanel.SetActive(true);
        }

        public bool IsConnectedToInternet => checkNetwork.Call<bool>("checkNetwork");

        public async void HearbeatNetwork()
        {
            int count = 0;
            while(count < 100)
            {
                await System.Threading.Tasks.Task.Delay(1000);
                networkIndicator.color = IsConnectedToInternet ? Color.green : Color.red;
                count++;
            }
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
                foreach(int actorNumber in room.players.Keys)
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
    }
}