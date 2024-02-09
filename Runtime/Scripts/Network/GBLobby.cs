using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
namespace Hashbyte.Multiplayer
{
    public class GBLobby : LobbyEventCallbacks
    {
        private ILobbyEvents lobbyEventListener;
        public LobbyServiceException CurrentException { get; private set; }


        public GBLobby(ILobbyEvents lobbyEvents = null)
        {
            lobbyEventListener = lobbyEvents;
            PlayerJoined += OnPlayerJoined;
        }

        private void OnPlayerJoined(System.Collections.Generic.List<LobbyPlayerJoined> playersJoined)
        {
            Debug.Log($"Players joined {playersJoined.Count}");
            lobbyEventListener?.OnPlayersJoined(playersJoined);
        }

        ~GBLobby()
        {
            PlayerJoined -= OnPlayerJoined;
        }

        public async Task<Lobby> CreateLobby(string gameSessionId, bool isPrivate = false)
        {
            try
            {
                CurrentException = null;               
                CreateLobbyOptions lobbyOptions = new CreateLobbyOptions();
                lobbyOptions.Data = new System.Collections.Generic.Dictionary<string, DataObject>() { { Constants.kJoinCode, new DataObject(DataObject.VisibilityOptions.Public, gameSessionId) } };
                lobbyOptions.IsPrivate = isPrivate;                
                Debug.Log("Creating new Lobby with data as game session Id");
                Lobby lobby = await LobbyService.Instance.CreateLobbyAsync("lobbyName", 2, options: lobbyOptions);
                Debug.Log($"New lobby created with ID {lobby.Id}");
                //Heartbeat lobby to keep it alive for 30 secs
                SubscribeToLobbyEvents(lobby);
                return lobby;
            }
            catch (LobbyServiceException e)
            {
                CurrentException = e;
                return null;
            }

        }       
        public async Task<Lobby> QuickJoinLobby(string passcode = "")
        {
            try
            {
                Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
                SubscribeToLobbyEvents(lobby);
                return lobby;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log($"No Lobbies found {e.Reason}");
                CurrentException = e;
                lobbyEventListener?.OnLobbyException(e.Reason, e.ApiError);
                return null;
            }
        }

        public string GetSessionId(Lobby lobby)
        {
            string gameSessionId = "";
            if (lobby != null)
            {               
                gameSessionId = lobby.Data[Constants.kJoinCode].Value;                
            }
            return gameSessionId;
        }
        private void SubscribeToLobbyEvents(Lobby lobby)
        {
            if (lobby == null) return;
            Lobbies.Instance.SubscribeToLobbyEventsAsync(lobby.Id, this);
        }
    }
}
