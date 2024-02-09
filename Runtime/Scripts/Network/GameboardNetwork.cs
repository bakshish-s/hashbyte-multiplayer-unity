using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public class GameboardNetwork : MonoBehaviour, ILobbyEvents, IRelayEvents
    {
        #region Singleton
        public static GameboardNetwork Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        #endregion

        private GBLobby lobbyService;
        private GBRelayNetwork relayNetwork;
        public Lobby CurrentRoom { get; private set; }
        public bool isHost => relayNetwork is HostNetwork;
        private void Initialize()
        {
            Unity.Collections.NativeLeakDetection.Mode = Unity.Collections.NativeLeakDetectionMode.EnabledWithStackTrace;
            lobbyService = new GBLobby(this);
        }

        #region Network Events
        public delegate void NetworkUpdate(NetworkState networkState, string data, GBNetworkError error);
        public event NetworkUpdate OnNetworkUpdate;
        public delegate void GameEvents(GameEvent gameEvent);
        public event GameEvents OnGameEvent;
        #endregion
        public async void JoinRandomGame()
        {
            try
            {
                await ConnectToNetwork();            //Connect to network if not already connected            
                CurrentRoom = await lobbyService.QuickJoinLobby();  //Try to join an already available lobby
                if (CurrentRoom != null)
                {
                    relayNetwork = new ClientNetwork(this); //Found a lobby, connect it to as a client
                    await ((ClientNetwork)relayNetwork).JoinGameSession(lobbyService.GetSessionId(CurrentRoom));
                }
                else //Create new lobby and become host
                {
                    relayNetwork = new HostNetwork(this);
                    string gameSessionId = await ((HostNetwork)relayNetwork).CreateGameSession(Constants.kMaxPlayers, "asia-south1");
                    CurrentRoom = await lobbyService.CreateLobby(gameSessionId);
                }
                OnNetworkUpdate?.Invoke(NetworkState.JOINED_LOBBY, CurrentRoom.LobbyCode, null);
            }catch(System.Exception e)
            {
                OnNetworkUpdate?.Invoke(NetworkState.ERROR, e.ToString(), new GBNetworkError() { errorMessage = e.Message});
            }
        }

        private void Update()
        {
            if (relayNetwork != null)
            {
                relayNetwork.NetworkUpdate();
            }
        }

        private void OnDestroy()
        {
            if (relayNetwork != null) relayNetwork.OnDestroy();
        }

        public void SendGameStartedEvent()
        {
            relayNetwork.SendEvent(GameEvent.GameStartEvent(AuthenticationService.Instance.PlayerId));
            OnNetworkUpdate?.Invoke(NetworkState.START_GAME, "", null);
        }

        public void SendGameMove(string data)
        {
            relayNetwork.SendEvent(GameEvent.GameMoveEvent(AuthenticationService.Instance.PlayerId, data));
        }

        public async Task ConnectToNetwork()
        {
            OnNetworkUpdate?.Invoke(NetworkState.CONNECTING, "", null);
            await UnityServices.InitializeAsync();//Initialize Unity Services            
            await AuthenticationService.Instance.SignInAnonymouslyAsync();//Sign In Anonymously
            OnNetworkUpdate?.Invoke(NetworkState.CONNECTED, $"{AuthenticationService.Instance.PlayerId}", null);
        }



        #region Interface Methods
        public void OnLobbyException(LobbyExceptionReason reason, ErrorStatus error)
        {
            OnNetworkUpdate?.Invoke(NetworkState.ERROR, "", new GBNetworkError() { errorCode = (int)reason, errorMessage = error.Detail });
        }

        public void OnPlayersJoined(List<LobbyPlayerJoined> playersJoined)
        {
            string playerJoinedMessage = "Joined Players\n";
            foreach (LobbyPlayerJoined playerJoined in playersJoined)
            {
                playerJoinedMessage += $"{playerJoined.Player.Id}\n";
            }
            OnNetworkUpdate?.Invoke(NetworkState.PLAYER_JOINED, playerJoinedMessage, null);
            //SendGameStartedEvent();
        }

        public void OnGameSessionCreated(string sessionId)
        {

        }

        public void OnConnectedToServer()
        {

        }

        public void OnNetworkError(NetworkErrorCode code, string message)
        {

        }

        public void OnGameSessionJoined()
        {

        }

        public void OnConnectedToHost()
        {
            OnNetworkUpdate?.Invoke(NetworkState.PLAYER_JOINED, "Joined to Host", null);
        }

        public void OnEvent(GameEvent gameEvent)
        {
            OnGameEvent?.Invoke(gameEvent);
            switch (gameEvent.eventType)
            {
                case GameEventType.GAME_STARTED:
                    OnNetworkUpdate?.Invoke(NetworkState.START_GAME, "", null);
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}
