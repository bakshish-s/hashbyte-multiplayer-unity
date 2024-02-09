using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    public class HashbyteNetwork
    {
        #region Singleton
        private static HashbyteNetwork instance;
        public static HashbyteNetwork Instance { get { if (instance == null) instance = new HashbyteNetwork(); return instance; } }
        #endregion

        #region Observer Events
        public delegate void GameEventListener(GameEvent gameEvent);
        public event GameEventListener OnGameJoined, OnGameStart, OnGameEnd, OnGameMove;
        public delegate void NetworkEventListener();
        public event NetworkEventListener OnConnectedToServer, OnConnectedToHost, OnJoinedRoom, OnPlayerEnter, OnPlayerLeft, OnRoomLeft, OnDisconnected;
        #endregion

        public bool IsInitialized { get; private set; }

        private MultiplayerService multiplayerService;

        public async Task Initialize(ServiceType serviceType)
        {
            switch (serviceType)
            {
                case ServiceType.UNITY:
                    multiplayerService = new UnityMultiplayerService();
                    break;
            }
            await multiplayerService.InitAndAuthenticate();
        }

        public async void JoinRandomGame()
        {
            if (!IsInitialized)
            {
                await Initialize(ServiceType.UNITY);    //If not initialized by client, consider Unity Service by default
            }
        }

        public void CreatePrivateGame()
        {

        }

        public void JoinPrivateGame()
        {

        }
    }
}
