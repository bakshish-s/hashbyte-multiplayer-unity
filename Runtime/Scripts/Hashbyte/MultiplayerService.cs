using System;
using System.Collections;
using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    //Responsibility: To provide access to different components of multiplayer services like Authentication, Lobby, Relay, Matchmaking etc.
    public class MultiplayerService : INetworkEvents
    {
        #region Singelton
        private static MultiplayerService _instance;
        public static MultiplayerService Instance { get { if (_instance == null) _instance = new MultiplayerService(ServiceType.UNITY); return _instance; } }
        #endregion
        public GameRoom CurrentRoom { get; private set; }
        public string PlayerId => authService.PlayerId;
        public bool IsHost => CurrentRoom.isHost;
        protected bool isInitialized => authService.IsInitialized;
        private IAuthService authService { get; set; }
        private IGameRoomService roomService { get; set; }
        private INetworkService networkService { get; set; }
        private IConnectSettings connectionSettings { get; set; }
        private INetworkPlayer networkPlayer { get; set; }

        public MultiplayerService(ServiceType serviceType)
        {
            switch (serviceType)
            {
                case ServiceType.UNITY:
                    authService = new UnityAuthService();
                    roomService = new UnityGameRoomService();
                    networkService = new UnityNetService();
                    connectionSettings = new UnityConnectSettings();
                    break;
                case ServiceType.EPIC:
                    break;
                case ServiceType.STEAM:
                    break;
                default:
                    break;
            }
            roomService.RegisterCallbacks(this);
        }

        public async Task Initialize(INetworkPlayer player)
        {
            if (isInitialized) return;
            if (player == null) await authService.Authenticate();
            else
            {
                networkPlayer = player;
                await authService.AuthenticateWith(player);
            }
        }

        public void RegisterPlayer(INetworkPlayer player)
        {
            networkPlayer = player;
            if (networkService.IsConnected)
            {
                networkPlayer.OnTurnUpdate(networkService.IsHost);
            }
        }
        public void RegisterCallbacks(INetworkEvents networkEventListener)
        {
            networkService.RegisterCallbacks(networkEventListener);
            roomService.RegisterCallbacks(networkEventListener);
        }

        public async void JoinOrCreateGame(Hashtable roomProperties = null)
        {
            await JoinOrCreateGameAsync(roomProperties);
        }

        public async Task<IRoomResponse> JoinOrCreateGameAsync(Hashtable roomProperties = null)
        {
            Debug.Log($"Auth Service initialization status {isInitialized}");
            if (!isInitialized) await Initialize(null);
            if (roomProperties == null) roomProperties = new Hashtable();
            if (networkPlayer != null && !string.IsNullOrEmpty(networkPlayer.PlayerId))
            {
                roomProperties.Add(Constants.kPlayerName, networkPlayer.PlayerId);
            }
            else
            {
                roomProperties.Add(Constants.kPlayerName, "Player_" + DateTime.UtcNow.Ticks.ToString());
            }
            IRoomResponse roomResponse = await roomService.JoinOrCreateRoom(roomProperties);
            if (roomResponse.Success)
            {
                connectionSettings.Initialize(Constants.kConnectionType, roomResponse);
                networkService.ConnectToServer(connectionSettings);
                CurrentRoom = roomResponse.Room;
                if (IsHost)
                {
                    //We created this room, but we should keep trying to join a room every 10 seconds until a game is not joined.
                    //We can leave this room if a game is joined
                    isGameJoined = false;
                    roomResponse = await TryJoinGame(roomResponse, roomProperties);
                }
            }
            else
            {
                Debug.Log($"Could not join or create room due to error {roomResponse.Error.Message}");
            }
            return roomResponse;
        }
        bool isGameJoined = false;
        float maxWaitTime = 60000;
        Random random = new Random();
        private async Task<IRoomResponse> TryJoinGame(IRoomResponse roomResponse, Hashtable options)
        {
            while (!isGameJoined && maxWaitTime > 0)
            {
                int waitDelay = random.Next(20, 100);
                Debug.Log($"Going to wait for {waitDelay} seconds");
                await Task.Delay(waitDelay * 100); //Wait for 2 seconds before trying to join a game
                maxWaitTime -= waitDelay * 100;
                if (isGameJoined)
                {
                    maxWaitTime = 60000;
                    isGameJoined = false;
                    return roomResponse;
                }
                System.Collections.Generic.List<string> availableRooms = await roomService.FindAvailableRooms();
                if (availableRooms.Count > 0)
                {
                    for (int i = 0; i < availableRooms.Count; i++)
                    {
                        if (availableRooms[i] != CurrentRoom.LobbyId)
                        {
                            roomResponse = await roomService.JoinRoom(availableRooms[i], options);
                            await roomService.DeleteRoom(CurrentRoom.LobbyId);
                            return roomResponse;
                        }

                    }
                }
            }
            maxWaitTime = 60000;
            ((UnityRoomResponse)roomResponse).Success = false;
            return roomResponse;
        }

        public void UpdateRoomProperties(Hashtable roomData)
        {
            roomService.UpdateRoomProperties(CurrentRoom.LobbyId, roomData);

        }

        public void SendMove(GameEvent gameMove)
        {
            networkService.SendMove(gameMove);
        }

        public void Update()
        {
            networkService?.NetworkUpdate();
        }

        public void CreatePrivateGame() { }
        public void JoinPrivateGame() { }
        public void Dispose()
        {
            networkService?.Dispose();
        }

        public void OnPlayerConnected()
        {
            //throw new System.NotImplementedException();
            isGameJoined = true;
        }

        public void OnRoomJoined(Hashtable roomProperties)
        {
            //throw new System.NotImplementedException();
        }

        public void OnRoomPropertiesUpdated(Hashtable roomProperties)
        {
            Debug.Log("Room Properties updated in MP Service");
            foreach (string roomProperty in roomProperties.Keys)
            {
                Debug.Log($"Property {roomProperty}, {roomProperties[roomProperty]}");
            }
        }

        public void OnNetworkMessage(GameEvent gameEvent)
        {
            //throw new System.NotImplementedException();
        }

        public void OnPlayerJoined(string joinedPlayerName)
        {
            isGameJoined = true;
            if(CurrentRoom != null)
            {
                CurrentRoom.AddPlayer(joinedPlayerName);
                Debug.Log($"Added newly joined player to room");
            }
        }
    }
}
