using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    public class UnityMultiplayerService : MultiplayerService
    {
        protected override IAuthService authService { get; set; }
        protected override IGameRoomService roomService { get; set; }
        protected override INetworkService networkService { get; set; }

        public UnityMultiplayerService()
        {
            authService = new UnityAuthService();
            roomService = new UnityGameRoomService();
        }

        public override async Task Initialize()
        {
            //Can be divided into two different methods
            await authService.Authenticate();
        }

        public override async Task<IRoomResponse> JoinRandomGame()
        {
            if (!isInitialized) await Initialize();
            UnityRoomResponse roomResponse = (UnityRoomResponse)(await roomService.JoinRandomRoom());
            networkService = roomResponse.isHost ? new UnityHostNetService() : new UnityClientNetService();
            UnityConnectSettings connectSettings = new UnityConnectSettings()
            {
                ConnectionType = "udp",
                roomResponse = roomResponse
            };
            networkService.ConnectToServer(connectSettings);
            return roomResponse;                        
        }

        public override void Update()
        {
            networkService?.NetworkUpdate();
        }
    }
}
