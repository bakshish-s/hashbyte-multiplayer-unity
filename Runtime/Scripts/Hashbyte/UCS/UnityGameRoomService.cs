using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Hashbyte.Multiplayer
{
    public class UnityGameRoomService : LobbyEventCallbacks, IGameRoomService, IRelayService, ILobbyService
    {
        private UnityRoomResponse roomResponse;
        private List<INetworkEvents> networkEventListeners;
        private Lobby createdLobby;

        public UnityGameRoomService()
        {
            PlayerJoined += OnPlayerJoined;
            DataChanged += LobbyDataChanged;
            LobbyDeleted += OnLobbyDeleted;            
        }        

        private void OnLobbyDeleted()
        {
            Debug.Log("Joined lobby has been deleted");
        }

        private void LobbyDataChanged(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> obj)
        {
            if (obj != null && obj.Count > 0)
            {
                Hashtable roomProperties = new Hashtable();
                foreach (string key in obj.Keys)
                {
                    roomProperties.Add(key, obj[key].Value);
                    Debug.Log($"Data added {key} -- {obj[key].Value}");
                }
                foreach (INetworkEvents netEventListener in networkEventListeners)
                    netEventListener.OnRoomPropertiesUpdated(roomProperties);
            }            
        }

        private void OnPlayerJoined(List<LobbyPlayerJoined> playersJoined)
        {
            foreach (INetworkEvents netEventListener in networkEventListeners)
            {
                foreach (LobbyPlayerJoined lobbyPlayer in playersJoined)
                {
                    Debug.Log($"Player Joined {lobbyPlayer.Player.Data[Constants.kPlayerName].Value}");
                    netEventListener.OnPlayerJoined(lobbyPlayer.Player.Data[Constants.kPlayerName].Value.ToString()/*lobbyPlayer.Player.Profile.Name*//*lobbyPlayer.PlayerIndex, lobbyPlayer.Player.Id, ""*/);
                }
            }            
        }

        public void RegisterCallbacks(INetworkEvents networkEvents)
        {
            if (networkEventListeners == null) networkEventListeners = new List<INetworkEvents>();
            networkEventListeners.Add(networkEvents);
        }

        public async Task<IRoomResponse> JoinOrCreateRoom(Hashtable roomProperties)
        {
            try
            {
                roomResponse = new UnityRoomResponse();
                string playerId = roomProperties[Constants.kPlayerName].ToString();
                //Try to join any awailable lobbies
                string roomId = await JoinLobby("", roomProperties);
                if (string.IsNullOrEmpty(roomId))
                {
                    Debug.Log($"Creating Room");
                    roomResponse = (UnityRoomResponse)(await CreateRoom(false, roomProperties));
                }
                else
                {
                    Debug.Log($"Joining Room {roomResponse.Room.LobbyId}");
                    await JoinRelaySession(roomId);
                    if (roomResponse.Room.RoomOptions != null)
                    {
                        await UpdateLobbyPlayer(roomResponse.Room.LobbyId, Unity.Services.Authentication.AuthenticationService.Instance.PlayerId, "Ravan");
                    }
                }
                return roomResponse;
            }
            catch (RelayServiceException exception)
            {
                Debug.Log($"Relay exception {exception.ErrorCode}, {exception.Message}");
                roomResponse = new UnityRoomResponse();
                roomResponse.Success = false;
                roomResponse.Error = new RoomError()
                {
                    ErrorCode = exception.ErrorCode,
                    Message = exception.Message,
                };
                return roomResponse;
            }
        }
        public async Task<IRoomResponse> CreateRoom(bool isPrivate, Hashtable roomProperties)
        {
            string sessionId = await CreateRelaySession(Constants.kRegionForServer);
            Debug.Log($"Relay session created {sessionId}");
            roomProperties.Add(Constants.kRoomId, sessionId);
            roomProperties.Add(Constants.kPlayers, roomProperties[Constants.kPlayerName].ToString());
            roomProperties.Remove(Constants.kPlayerName);
            string lobbyId = await CreateLobby(sessionId, Constants.kMaxPlayers, roomProperties);
            GameRoom room = new GameRoom(roomId: sessionId, lobbyId: lobbyId, isHost: true, options: roomProperties);
            roomResponse.Success = true;
            Debug.Log($"Lobby Created {room.LobbyId}");
            roomResponse.Room = room;
            return roomResponse;
        }
        public async Task<string> CreateRelaySession(string region)
        {
            Allocation relayAllocation = await RelayService.Instance.CreateAllocationAsync(Constants.kMaxPlayers, region);
            roomResponse.hostAllocation = relayAllocation;
            return await RelayService.Instance.GetJoinCodeAsync(relayAllocation.AllocationId);
        }
        public async Task JoinRelaySession(string sessionId)
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(sessionId);
            roomResponse.clientAllocation = allocation;
            roomResponse.Success = true;
        }

        public async Task<string> CreateLobby(string lobbyName, int maxPlayers, Hashtable roomProperties)
        {
            Dictionary<string, DataObject> data = new Dictionary<string, DataObject>();
            foreach (string key in roomProperties.Keys)
            {
                DataObject dataObject = new DataObject(visibility: DataObject.VisibilityOptions.Public,
                    value: roomProperties[key].ToString());
                data.Add(key, dataObject);
            }
            Player player = new Player()
            {
                Data = new Dictionary<string, PlayerDataObject> { { Constants.kPlayerName, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, roomProperties[Constants.kPlayers].ToString()) } },
            };
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                Data = data,
                Player = player
            };
            createdLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);
            try
            {
                await LobbyService.Instance.SubscribeToLobbyEventsAsync(createdLobby.Id, this);
            }
            catch (LobbyServiceException ex)
            {
                Debug.Log(ex.Reason.ToString());
            }            
            return createdLobby.Id;
        }

        public async Task<string> JoinLobby(string lobbyId, Hashtable options)
        {
            try
            {
                Lobby lobby;
                Player lobbyPlayer = new Player()
                {
                    Data = new Dictionary<string, PlayerDataObject> { { Constants.kPlayerName, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, options[Constants.kPlayerName].ToString()) } }
                };                
                if (string.IsNullOrEmpty(lobbyId))
                {
                    QuickJoinLobbyOptions quickJoinOptions = new QuickJoinLobbyOptions() { Player = lobbyPlayer };
                    lobby = await LobbyService.Instance.QuickJoinLobbyAsync(quickJoinOptions);
                }
                else
                {
                    JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions() { Player = lobbyPlayer };
                    lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
                }
                if (lobby != null)
                {
                    for (int i = 0; i < lobby.Players.Count; i++)
                    {
                        Debug.Log($"Player in lobby {lobby.Players[i].Id} -- Profile {lobby.Players[i].Data[Constants.kPlayerName]}");
                    }
                }
                foreach (string key in lobby.Data.Keys)
                {
                    Debug.Log($"Getting data from joined lobby {key}, {lobby.Data[key].Value}");
                    if (options.ContainsKey(key))
                        options[key] = lobby.Data[key];
                    else
                        options.Add(key, lobby.Data[key].Value);
                }
                if (options.ContainsKey(Constants.kPlayers))
                {
                    string existingPlayers = options[Constants.kPlayers].ToString();
                    existingPlayers += ":" + options[Constants.kPlayerName];
                    options[Constants.kPlayers] = existingPlayers;
                    options.Remove(Constants.kPlayerName);
                }
                GameRoom room = new GameRoom(roomId: lobby.Data[Constants.kRoomId].Value, lobbyId: lobby.Id, isHost: false, options: options);
                try
                {
                    await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, this);
                }
                catch (LobbyServiceException ex)
                {
                    Debug.Log(ex.Reason.ToString());
                }
                
                roomResponse.Room = room;
                return lobby == null ? "" : lobby.Data[Constants.kRoomId].Value;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e.Reason.ToString());
                return null;
            }
        }

        public async Task UpdateLobbyData(string lobbyId, Hashtable dataToUpdate)
        {
            UpdateLobbyOptions options = new UpdateLobbyOptions();
            options.Data = new Dictionary<string, DataObject>();
            foreach (var dataObj in dataToUpdate.Keys)
            {
                options.Data.Add(dataObj.ToString(), new DataObject(visibility: DataObject.VisibilityOptions.Public, value: dataToUpdate[dataObj].ToString()));
            }
            try
            {
                Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(lobbyId, options);                
            }
            catch (LobbyServiceException ex)
            {
                Debug.Log(ex.Reason.ToString());
            }
        }

        private async Task UpdateLobbyPlayer(string lobbyId, string playerId, string playerName)
        {
            UpdatePlayerOptions options = new UpdatePlayerOptions();
            options.Data = new Dictionary<string, PlayerDataObject>()
            {
                {"PlayerName", new PlayerDataObject(visibility: PlayerDataObject.VisibilityOptions.Public, playerName) },                
            };
            options.Data.Add("Hash", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, "Byte"));
            try
            {
                Lobby lobby = await LobbyService.Instance.UpdatePlayerAsync(lobbyId, playerId, options);
                Debug.Log($"Lobby updated {lobby}");                      
            }
            catch (LobbyServiceException ex)
            {
                Debug.Log(ex.Reason.ToString());
            }
        }

        public async Task DeleteRoom(string roomId)
        {
            await LobbyService.Instance.DeleteLobbyAsync(roomId);
            Debug.Log($"Room Deleted {roomId}");
        }

        public async Task<List<string>> FindAvailableRooms()
        {
            QueryLobbiesOptions queryOptions = new QueryLobbiesOptions();
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
            List<string> availableRooms = new List<string>();
            List<Lobby> availableLobbies = response.Results;
            Debug.Log($"found rooms {availableLobbies.Count}");
            foreach (Lobby lobby in availableLobbies)
            {
                if (lobby.Players.Count < 2)
                {
                    Debug.Log($"Adding rooms {lobby.Id}");
                    availableRooms.Add(lobby.Id);
                }
            }
            return availableRooms;
        }
        public async Task<IRoomResponse> JoinRoom(string roomId, System.Collections.Hashtable options)
        {
            roomResponse = new UnityRoomResponse();
            string rId = await JoinLobby(roomId, options);
            Debug.Log($"On Joining lobby {roomId}");
            if (!string.IsNullOrEmpty(rId))
            {
                await JoinRelaySession(rId);
            }
            return roomResponse;
        }

        public async Task UpdateRoomProperties(string roomID, Hashtable roomProperties)
        {
            await UpdateLobbyData(roomID, roomProperties);
        }
    }
}
