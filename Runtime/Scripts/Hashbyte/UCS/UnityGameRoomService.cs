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
        public event IGameRoomService.RoomJoined OnRoomJoined;
        private UnityRoomResponse roomResponse;
        private List<INetworkEvents> networkEventListeners;
        public UnityGameRoomService()
        {
            PlayerJoined += OnPlayerJoined;
        }

        private void OnPlayerJoined(List<LobbyPlayerJoined> playersJoined)
        {
            foreach (INetworkEvents netEventListener in networkEventListeners)
                foreach (LobbyPlayerJoined lobbyPlayer in playersJoined)
                    netEventListener.OnPlayerJoined(/*lobbyPlayer.PlayerIndex, lobbyPlayer.Player.Id, ""*/);
        }

        public void RegisterCallbacks(INetworkEvents networkEvents)
        {
            Debug.Log("Initializing room call backs");
            if (networkEventListeners == null) networkEventListeners = new List<INetworkEvents>();
            networkEventListeners.Add(networkEvents);
        }

        public async Task<IRoomResponse> CreateRoom(bool isPrivate)
        {
            Debug.Log($"Creating new room");
            roomResponse = new UnityRoomResponse();
            string sessionId = await CreateRelaySession(Constants.kRegionForServer);
            Debug.Log($"Relay Session created {sessionId}");
            await CreateLobby(sessionId, Constants.kMaxPlayers, sessionId);
            Debug.Log($"New lobby created");
            roomResponse.RoomId = sessionId;
            roomResponse.Success = true;
            return roomResponse;
        }

        public async Task<IRoomResponse> JoinRandomRoom()
        {
            Debug.Log($"Joining random room ");
            try
            {
                string roomId = await JoinLobby("", "");
                if (string.IsNullOrEmpty(roomId))
                {
                    return await CreateRoom(false);
                }
                else
                {
                    roomResponse = new UnityRoomResponse();
                    await JoinRelaySession(roomId);
                    return roomResponse;
                }
            }catch(RelayServiceException exception)
            {
                roomResponse = new UnityRoomResponse();
                roomResponse.Success=false;
                roomResponse.Error = new RoomError()
                {
                    ErrorCode = exception.ErrorCode,
                    Message = exception.Message,
                };
                return roomResponse;
            }
        }
        public async Task<string> CreateRelaySession(string region)
        {
            Allocation relayAllocation = await RelayService.Instance.CreateAllocationAsync(Constants.kMaxPlayers, region);
            roomResponse.isHost = true;
            roomResponse.hostAllocation = relayAllocation;
            return await RelayService.Instance.GetJoinCodeAsync(relayAllocation.AllocationId);
        }
        public async Task JoinRelaySession(string sessionId)
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(sessionId);
            roomResponse.isHost = false;
            roomResponse.clientAllocation = allocation;
            roomResponse.Success = true;
            roomResponse.RoomId = sessionId;
        }

        public async Task CreateLobby(string lobbyName, int maxPlayers, object additionalData)
        {
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                Data = new Dictionary<string, DataObject> { { Constants.kRoomId, new DataObject(DataObject.VisibilityOptions.Public, additionalData.ToString()) } }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);
            await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, this);
        }

        public async Task<string> JoinLobby(string lobbyId, object additionalData)
        {
            try
            {
                Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
                Debug.Log($"Lobby found {lobby}");
                return lobby == null ? "" : lobby.Data[Constants.kRoomId].Value;
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e.Reason.ToString());
                return null;
            }
        }

        public Task UpdateLobbyData(string lobbyId)
        {
            throw new System.NotImplementedException();
        }
    }
}
