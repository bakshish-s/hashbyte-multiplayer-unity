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

        public override async Task<string> JoinRandomGame()
        {
            if (!isInitialized) await Initialize();
            string roomId = await roomService.JoinRandomRoom();
            networkService.ConnectToServer();
            return roomId;                        
        }

        public override void Update()
        {
            networkService?.NetworkUpdate();
        }
    }
}
