using System;
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
        public event NetworkEventListener OnConnectedToServer, OnConnectedToHost, OnJoinedRoom, OnPlayerEnter, OnPlayerLeft, OnRoomLeft, OnDisconnected, OnInitialized, OnError;
        #endregion

        public bool IsInitialized { get; private set; }

        private MultiplayerService multiplayerService;
        private bool isInitializing;

        public async void Initialize(ServiceType serviceType)
        {
            await InitializeAsync(serviceType);
        }

        public async Task InitializeAsync(ServiceType serviceType)
        {
            if(isInitializing) return;
            if (IsInitialized) { OnInitialized?.Invoke(); return; }
            isInitializing = true;
            switch (serviceType)
            {
                case ServiceType.UNITY:
                    multiplayerService = new UnityMultiplayerService();
                    break;
            }
            await multiplayerService.Initialize();
            OnInitialized?.Invoke();
            IsInitialized = false;
        }

        public async void JoinRandomGame()
        {
            await JoinRandomGameAsync();
        }

        public async Task JoinRandomGameAsync()
        {
            if (!IsInitialized)
            {
                await InitializeAsync(ServiceType.UNITY);    //If not initialized by client, consider Unity Service by default
            }
            //After initialization succesfull, try to join a game if available
            string roomId = await multiplayerService.JoinRandomGame();
            if(string.IsNullOrEmpty(roomId))
            {
                Debug.Log("Error joining game. Get details in OnError event if subscribed");
                OnError?.Invoke();
            }
            else
            {
                OnJoinedRoom?.Invoke();
            }
        }        

        public void CreatePrivateGame()
        {

        }

        public void JoinPrivateGame()
        {

        }

        public void Update()
        {
            multiplayerService?.Update();
        }
    }
}
