using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    //Responsibility: To provide access to different components of multiplayer services like Authentication, Lobby, Relay, Matchmaking etc.
    public class MultiplayerService
    {
        protected bool isInitialized => authService.IsInitialized;
        private IAuthService authService { get; set; }
        private IGameRoomService roomService { get; set; }
        private INetworkService networkService { get; set; }
        private IConnectSettings connectionSettings { get; set; }

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

        public async Task Initialize()
        {
            //Can be divided into two different methods
            await authService.Authenticate();
        }

        public async Task<IRoomResponse> JoinRandomGame()
        {
            Debug.Log($"Auth Service initialization status {isInitialized}");
            if (!isInitialized) await Initialize();
            IRoomResponse roomResponse = await roomService.JoinRandomRoom();
            connectionSettings.Initialize(Constants.kConnectionType, roomResponse);
            networkService.ConnectToServer(connectionSettings);
            return roomResponse;
        }

        public void Update()
        {
            networkService?.NetworkUpdate();
        }
        public void CreatePrivateGame() { }
        public void JoinPrivateGame() { }
        public void OnPlayerJoined() { }
        public void OnPlayerLeft() { }
        public void OnPlayerDisconnected() { }
        public void Dispose()
        {
            networkService?.Dispose();
        }
    }
}
