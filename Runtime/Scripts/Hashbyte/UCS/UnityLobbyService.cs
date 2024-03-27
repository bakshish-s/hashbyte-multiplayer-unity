
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;

namespace Hashbyte.Multiplayer
{
    internal class UnityLobbyService : LobbyEventCallbacks
    {
        public delegate void LobbyPlayersJoined(List<INetworkPlayer> players);
        public event LobbyPlayersJoined OnPlayersJoined;
        public delegate void LobbyPlayersLeft(List<int> playerIndices);
        public event LobbyPlayersLeft OnPlayersLeft;
        public delegate void LobbyDeletedDelegate();
        public event LobbyDeletedDelegate OnLobbyDeleted;

        private string lobbyId;
        private int lobbyLifespan = 120;
        public UnityLobbyService()
        {
            PlayerJoined += OnPlayerJoined;
            PlayerLeft += OnPlayerLeft;
            LobbyDeleted += () =>
            {
                OnLobbyDeleted?.Invoke();
                lobbyId = null;
            };
        }

        private void OnPlayerLeft(List<int> playersLeft)
        {
            if (playersLeft.Count > 0)
            {
                Debug.Log($"Players left {playersLeft[0]}");
                OnPlayersLeft?.Invoke(playersLeft);
            }
        }

        public async Task<IRoomResponse> CreateLobby(Hashtable roomProperties, bool isPrivate, UnityRoomResponse relayResponse)
        {
            lobbyId = null;
            Dictionary<string, DataObject> data = new Dictionary<string, DataObject>() { { Constants.kRoomId, new DataObject(DataObject.VisibilityOptions.Public, relayResponse.Room.RoomId) } };
            if (roomProperties != null)
            {
                foreach (string key in roomProperties.Keys)
                    data.Add(key, new DataObject(DataObject.VisibilityOptions.Member, roomProperties[key].ToString()));
            }
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                Data = data,
                Player = GetLobbyPlayer(roomProperties[Constants.kPlayerName].ToString()),
                IsPrivate = isPrivate,
            };
            try
            {
                Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(roomProperties[Constants.kPlayerName].ToString(), 2, options);
                lobbyId = lobby.Id;
                HeartbeatLobby();
                Debug.Log($"Lobby {lobby.LobbyCode} {lobby.Id}");
                await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, this);
                IRoomResponse roomResponse = GetSuccessRoomResponse(lobby, true);
                ((UnityRoomResponse)roomResponse).hostAllocation = relayResponse.hostAllocation;
                return roomResponse;
            }
            catch (LobbyServiceException ex)
            {
                Debug.Log(ex.Reason.ToString());
                return GetFailureRoomResponse(ex);
            }
        }

        private async void HeartbeatLobby()
        {
            try
            {
                while (lobbyLifespan > 0 && !string.IsNullOrEmpty(lobbyId))
                {
                    await Task.Delay(20000);
                    lobbyLifespan -= 1;
                    if (string.IsNullOrEmpty(lobbyId)) break;
                    await LobbyService.Instance.SendHeartbeatPingAsync(lobbyId);
                }
            }
            catch (LobbyServiceException ex)
            {
                Debug.Log(ex.Reason.ToString());
            }
        }

        public async Task<IRoomResponse> JoinLobbyByCode(string lobbyCode, Hashtable options)
        {
            JoinLobbyByCodeOptions joinOptions = new JoinLobbyByCodeOptions();
            joinOptions.Player = GetLobbyPlayer(options[Constants.kPlayerName].ToString());
            try
            {
                Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOptions);
                await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, this);
                return GetSuccessRoomResponse(lobby, false);
            }
            catch (LobbyServiceException ex)
            {
                Debug.Log(ex.Reason.ToString());
                return GetFailureRoomResponse(ex);
            }
        }

        public async Task<IRoomResponse> JoinLobbyById(string lobbyId, Hashtable options)
        {
            JoinLobbyByIdOptions joinOptions = new JoinLobbyByIdOptions();
            joinOptions.Player = GetLobbyPlayer(options[Constants.kPlayerName].ToString());
            try
            {
                Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, joinOptions);
                await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, this);
                return GetSuccessRoomResponse(lobby, false);
            }
            catch (LobbyServiceException ex)
            {
                Debug.Log(ex.Reason.ToString());
                return GetFailureRoomResponse(ex);
            }
        }

        public async Task<IRoomResponse> QuickJoinLobby(Hashtable options)
        {
            QuickJoinLobbyOptions joinOptions = new QuickJoinLobbyOptions();
            joinOptions.Player = GetLobbyPlayer(options[Constants.kPlayerName].ToString());
            try
            {
                Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(joinOptions);
                await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, this);
                return GetSuccessRoomResponse(lobby, false);
            }
            catch (LobbyServiceException ex)
            {
                Debug.Log(ex.Reason.ToString());
                return GetFailureRoomResponse(ex);
            }
        }

        private IRoomResponse GetSuccessRoomResponse(Lobby lobby, bool isHost)
        {
            GameRoom room = GetGameRoom(lobby, isHost);
            return new UnityRoomResponse() { Room = room, Success = true };
        }
        private IRoomResponse GetFailureRoomResponse(LobbyServiceException exception)
        {
            return new UnityRoomResponse() { Success = false, Error = new RoomError() { Message = exception.Message, ErrorCode = exception.ErrorCode } };
        }
        private GameRoom GetGameRoom(Lobby lobby, bool isHost)
        {
            GameRoom gameRoom = new GameRoom();
            gameRoom.isPrivateRoom = lobby.IsPrivate;
            foreach (string key in lobby.Data.Keys)
            {
                gameRoom.RoomOptions.Add(key, lobby.Data[key].Value);
            }
            for (int i = 0; i < lobby.Players.Count; i++)
            {
                gameRoom.AddPlayer(new NetworkPlayer()
                {
                    PlayerName = lobby.Players[i].Data[Constants.kPlayerName].Value,
                    ActorNumber = i + 1,
                    PlayerId = lobby.Players[i].Data[Constants.kPlayerName].Value
                });
            }
            gameRoom.RoomId = lobby.Data[Constants.kRoomId].Value;
            gameRoom.LobbyId = lobby.Id;
            gameRoom.LobbyCode = lobby.LobbyCode;
            gameRoom.isHost = isHost;
            return gameRoom;
        }
        public async Task DeleteLobby(string lobbyId)
        {
            await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
            lobbyId = null;
        }

        public async Task LeaveLobby(string lobbyId, string playerId)
        {
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, playerId);

        }

        public async Task GetAvailableLobbies()
        {
            QueryResponse findLobbiesResponse = await LobbyService.Instance.QueryLobbiesAsync();
            if (findLobbiesResponse.Results != null)
            {
                for (int i = 0; i < findLobbiesResponse.Results.Count; i++)
                {
                    Debug.Log($"Lobby found {findLobbiesResponse.Results[i].LobbyCode}");
                }
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

        public async Task UpdateLobbyPlayer(string lobbyId, string playerId, string playerName)
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

        private void OnPlayerJoined(List<LobbyPlayerJoined> playersJoined)
        {
            List<INetworkPlayer> playerJoinedList = new List<INetworkPlayer>();
            foreach (LobbyPlayerJoined lobbyPlayer in playersJoined)
            {
                Debug.Log($"Player Joined {lobbyPlayer.Player.Data[Constants.kPlayerName].Value}");
                playerJoinedList.Add(new NetworkPlayer()
                {
                    PlayerName = lobbyPlayer.Player.Data[Constants.kPlayerName].Value,
                    ActorNumber = lobbyPlayer.PlayerIndex + 1,
                    PlayerId = lobbyPlayer.Player.Data[Constants.kPlayerId].Value
                });
            }
            OnPlayersJoined?.Invoke(playerJoinedList);
        }
        private Player GetLobbyPlayer(string playerName)
        {
            Player lobbyPlayer = new Player();
            lobbyPlayer.Data = new Dictionary<string, PlayerDataObject>() { { Constants.kPlayerName, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, playerName) },
                {Constants.kPlayerId, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, Unity.Services.Authentication.AuthenticationService.Instance.PlayerId) } };
            return lobbyPlayer;
        }

    }
}
