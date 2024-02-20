using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    //Responsibility: To provide access to different components of multiplayer services like Authentication, Lobby, Relay, Matchmaking etc.
    public class MultiplayerService
    {
        #region Singelton
        private static MultiplayerService _instance;
        public static MultiplayerService Instance { get { if (_instance == null) _instance = new MultiplayerService(ServiceType.UNITY); return _instance; } }
        #endregion
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
        }

        public async Task Initialize(INetworkPlayer player)
        {
            if(isInitialized) return;
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

        public async Task<IRoomResponse> JoinRandomGame()
        {
            Debug.Log($"Auth Service initialization status {isInitialized}");
            if (!isInitialized) await Initialize(null);
            IRoomResponse roomResponse = await roomService.JoinRandomRoom();
            connectionSettings.Initialize(Constants.kConnectionType, roomResponse);
            networkService.ConnectToServer(connectionSettings);           
            return roomResponse;
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
    }
}
