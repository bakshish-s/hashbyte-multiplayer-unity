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
        private UnityRelayService relayService;
        private UnityLobbyService lobbyService;

        public UnityGameRoomService(IMultiplayerEvents multiplayerEvents)
        {
            relayService = new UnityRelayService();
            lobbyService = new UnityLobbyService();
            if (multiplayerEvents != null)
            {
                multiplayerEventListener = multiplayerEvents;
                lobbyService.OnPlayersJoined += multiplayerEvents.OnPlayerJoinedRoom;
                lobbyService.OnPlayersLeft += multiplayerEvents.OnPlayerLeftRoom;
                lobbyService.OnLobbyDeleted += multiplayerEvents.OnRoomDeleted;
            }
        }

        public async Task<IRoomResponse> JoinOrCreateRoom(Hashtable roomProperties)
        {
            roomResponse = await lobbyService.QuickJoinLobby(roomProperties);
            if (roomResponse.Success)
            {
                roomResponse = await relayService.JoinRelaySession((UnityRoomResponse)roomResponse);
                multiplayerEventListener?.JoinRoomResponse(roomResponse);
            }
            else
            {
                roomResponse = await CreateRoom(false, roomProperties);
            }
            return roomResponse;


        }
        public async Task<IRoomResponse> CreateRoom(bool isPrivate, Hashtable roomProperties)
        {
            roomResponse = await relayService.CreateRelaySession(Constants.kRegionForServer, new UnityRoomResponse() { Room = new GameRoom() });
            roomResponse = await lobbyService.CreateLobby(roomProperties, isPrivate, (UnityRoomResponse)roomResponse);
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
        public async Task<IRoomResponse> JoinRoom(string roomId, Hashtable options)
        {
            roomResponse = await lobbyService.JoinLobbyById(roomId, options);
            roomResponse = await relayService.JoinRelaySession((UnityRoomResponse)roomResponse);
            multiplayerEventListener?.JoinRoomResponse(roomResponse);
            return roomResponse;
        }
        public async Task<IRoomResponse> JoinRoomByCode(string roomCode, Hashtable options)
        {
            roomResponse = await lobbyService.JoinLobbyByCode(roomCode, options);
            roomResponse = await relayService.JoinRelaySession((UnityRoomResponse)roomResponse);
            multiplayerEventListener?.JoinRoomResponse(roomResponse);
            return roomResponse;
        }

        public async Task UpdateRoomProperties(string roomID, Hashtable roomProperties)
        {
            await lobbyService.UpdateLobbyData(roomID, roomProperties);
        }

        public async Task DeleteRoom(GameRoom room)
        {
            await lobbyService.DeleteLobby(room.LobbyId);
        }
    }
}
