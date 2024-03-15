using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Hashbyte.Multiplayer
{
    //Responsibility: To provide access to different components of multiplayer services like Authentication, Lobby, Relay, Matchmaking etc.
    public class MultiplayerService : IMultiplayerEvents
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
        private List<INetworkEvents> networkListeners;

        public MultiplayerService(ServiceType serviceType)
        {
            switch (serviceType)
            {
                case ServiceType.UNITY:
                    authService = new UnityAuthService();
                    roomService = new UnityGameRoomService(this);
                    networkService = new UnityNetService(this);
                    connectionSettings = new UnityConnectSettings();
                    break;
                case ServiceType.EPIC:
                    break;
                case ServiceType.STEAM:
                    break;
                default:
                    break;
            }
            networkListeners = new List<INetworkEvents>();
        }

        public async Task Initialize(string playerId)
        {
            if (isInitialized) return;
            if (string.IsNullOrEmpty(playerId)) playerId = "Player_" + System.Guid.NewGuid().ToString();
            networkPlayer = new NetworkPlayer() { PlayerId = playerId, };
            await authService.AuthenticateWith(networkPlayer);
        }

        public void RegisterCallbacks(INetworkEvents networkEventListener)
        {            
            networkListeners.Add(networkEventListener);
        }

        public async void JoinOrCreateGame(Hashtable roomProperties = null)
        {
            await JoinOrCreateGameAsync(roomProperties);
        }

        public async Task<IRoomResponse> JoinOrCreateGameAsync(Hashtable roomProperties = null)
        {
            Debug.Log($"Auth Service initialization status {isInitialized}");
            if (!isInitialized) await Initialize(null);
            if (roomProperties == null) roomProperties = new Hashtable() { {Constants.kPlayerName, networkPlayer.PlayerId } };            
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

        public async void CreatePrivateGame(Hashtable gameOptions)
        {
            await CreatePrivateGameAsync(gameOptions);
        }

        public async Task<string> CreatePrivateGameAsync(Hashtable gameOptions)
        {
            if (gameOptions == null) gameOptions = new Hashtable();
            if (networkPlayer != null && !string.IsNullOrEmpty(networkPlayer.PlayerId))
            {
                gameOptions.Add(Constants.kPlayerName, networkPlayer.PlayerId);
            }
            else
            {
                gameOptions.Add(Constants.kPlayerName, "Player_" + DateTime.UtcNow.Ticks.ToString());
            }
            IRoomResponse roomResponse = await roomService.CreateRoom(true, gameOptions);
            return roomResponse.Room.RoomId;
        }
        public async Task<IRoomResponse> JoinPrivateGameAsync(string passcode)
        {
            Hashtable gameOptions = new Hashtable();
            if (networkPlayer != null && !string.IsNullOrEmpty(networkPlayer.PlayerId))
            {
                gameOptions.Add(Constants.kPlayerName, networkPlayer.PlayerId);
            }
            else
            {
                gameOptions.Add(Constants.kPlayerName, "Player_" + DateTime.UtcNow.Ticks.ToString());
            }
            return await roomService.JoinRoom(passcode, gameOptions);
        }
        public void Dispose()
        {
            networkService?.Dispose();
        }

        public void OnPlayerConnected()
        {            
            isGameJoined = true;
            //Ready to start the game
        }        

        public void JoinRoomResponse(IRoomResponse roomResponse)
        {
            Debug.Log($"Room Join status {roomResponse.Success}");
            if (roomResponse.Success)
            {
                CurrentRoom = roomResponse.Room;
                foreach (INetworkEvents networkListener in networkListeners)
                {
                    networkListener.OnRoomJoined(roomResponse.Room);
                }
            }
        }

        public void CreateRoomResponse(IRoomResponse roomResponse)
        {

        }
        /// <summary>
        /// Called when a player joins room created by this player
        /// </summary>
        /// <param name="playersJoined"></param>
        public void OnPlayerJoinedRoom(List<string> playersJoined)
        {
            isGameJoined = true;
            if (CurrentRoom != null)
            {
                foreach (string playerName in playersJoined)
                {
                    CurrentRoom.AddPlayer(new NetworkPlayer() { PlayerName = playerName });
                }
            }
        }

        public List<INetworkEvents> GetTurnEventListeners()
        {
            return networkListeners;
        }
    }
}
