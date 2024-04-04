using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;


namespace Hashbyte.Multiplayer
{
    internal class UnityGameRoomService : IGameRoomService
    {
        private IRoomResponse roomResponse;
        private IMultiplayerEvents multiplayerEventListener;
        private UnityLobbyService lobbyService;

        public UnityGameRoomService(IMultiplayerEvents multiplayerEvents)
        {
            lobbyService = new UnityLobbyService();
            if (multiplayerEvents != null)
            {
                multiplayerEventListener = multiplayerEvents;
                lobbyService.OnPlayersJoined += multiplayerEvents.OnPlayerJoinedRoom;
                lobbyService.OnPlayersLeft += multiplayerEvents.OnPlayerLeftRoom;
                lobbyService.OnLobbyDeleted += multiplayerEvents.OnRoomDeleted;
                lobbyService.OnDataUpdated += multiplayerEvents.OnRoomDataUpdated;
                lobbyService.OnJoinFailure += multiplayerEvents.OnRoomJoinFailed;
            }
        }

        public async Task<IRoomResponse> JoinOrCreateRoom(Hashtable roomProperties, Hashtable playerProperties)
        {
            roomResponse = await lobbyService.QuickJoinLobby(roomProperties, playerProperties);
            if (roomResponse.Success)
            {
                if (await UnityRelayService.Instance.JoinRelaySession(roomResponse.Room.RoomId))
                    ((UnityRoomResponse)roomResponse).clientAllocation = UnityRelayService.Instance.ClientAllocation;
                else
                    roomResponse = new UnityRoomResponse() { Success = false, Error = new RoomError() { Message = "Check log for more details" } };
                multiplayerEventListener?.JoinRoomResponse(roomResponse);
            }
            else
            {
                roomResponse = await CreateRoom(false, roomProperties, playerProperties);
            }
            return roomResponse;


        }
        public async Task<IRoomResponse> CreateRoom(bool isPrivate, Hashtable roomProperties, Hashtable playerOptions)
        {
            bool success = await UnityRelayService.Instance.CreateRelaySession();
            if (success)
            {
                ((UnityRoomResponse)roomResponse).hostAllocation = UnityRelayService.Instance.HostAllocation;
                ((UnityRoomResponse)roomResponse).Room = new GameRoom() { RoomId = UnityRelayService.Instance.JoinCode };                
            }
            else
            {
                roomResponse = new UnityRoomResponse() { Success = false, Error = new RoomError() { Message = "Check log for more details" } };
            }
            roomResponse = await lobbyService.CreateLobby(roomProperties, playerOptions, isPrivate, (UnityRoomResponse)roomResponse);
            multiplayerEventListener?.JoinRoomResponse(roomResponse);
            return roomResponse;
        }

        public async Task LeaveRoom(GameRoom room)
        {
            //leave Lobby and disconnect from Relay
            await lobbyService.LeaveLobby(room.LobbyId, Unity.Services.Authentication.AuthenticationService.Instance.PlayerId);
        }

        public async Task<List<string>> FindAvailableRooms()
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
            List<string> availableRooms = new List<string>();
            List<Lobby> availableLobbies = response.Results;
            Debug.Log($"found rooms {availableLobbies.Count}");
            foreach (Lobby lobby in availableLobbies)
            {
                if (lobby.Players.Count < 2)
                {
                    Debug.Log($"Adding rooms {lobby.LobbyCode} -- {lobby.Id} -- {lobby.Players.Count} --" +
                        $"{lobby.MaxPlayers} -- {lobby.Data[Constants.kRoomId].Value}");
                    availableRooms.Add(lobby.LobbyCode);
                }
            }
            return availableRooms;
        }
        public async Task<IRoomResponse> JoinRoom(string roomId, Hashtable options, Hashtable playerOptions)
        {
            roomResponse = await lobbyService.JoinLobbyById(roomId, options, playerOptions);
            if (await UnityRelayService.Instance.JoinRelaySession(roomResponse.Room.RoomId))
                ((UnityRoomResponse)roomResponse).clientAllocation = UnityRelayService.Instance.ClientAllocation;
            else
                roomResponse = new UnityRoomResponse() { Success = false, Error = new RoomError() { Message = "Check log for more details" } };
            multiplayerEventListener?.JoinRoomResponse(roomResponse);
            return roomResponse;
        }
        public async Task<IRoomResponse> JoinRoomByCode(string roomCode, Hashtable options, Hashtable playerOptions)
        {
            roomResponse = await lobbyService.JoinLobbyByCode(roomCode, options, playerOptions);
            if (roomResponse.Success && await UnityRelayService.Instance.JoinRelaySession(roomResponse.Room.RoomId))
                ((UnityRoomResponse)roomResponse).clientAllocation = UnityRelayService.Instance.ClientAllocation;
            else
                roomResponse = new UnityRoomResponse() { Success = false, Error = new RoomError() { Message = "Check log for more details" } };
            multiplayerEventListener?.JoinRoomResponse(roomResponse);            
            return roomResponse;
        }

        public async Task<Hashtable> UpdateRoomProperties(string roomID, Hashtable roomProperties)
        {
            Lobby lobby = await lobbyService.UpdateLobbyData(roomID, roomProperties);
            Hashtable updatedProperties = new Hashtable();
            if (lobby != null)
            {
                foreach (string dataKey in lobby.Data.Keys)
                {
                    updatedProperties.Add(dataKey, lobby.Data[dataKey].Value);
                }
            }
            return updatedProperties;
        }

        public async Task DeleteRoom(GameRoom room)
        {
            await lobbyService.DeleteLobby(room.LobbyId);
        }
    }
}
