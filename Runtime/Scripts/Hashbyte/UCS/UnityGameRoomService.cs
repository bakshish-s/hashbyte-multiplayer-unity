using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Hashbyte.Multiplayer
{
    public class UnityGameRoomService : IGameRoomService, IRelayService, ILobbyService
    {
        public event IGameRoomService.RoomJoined OnRoomJoined;

        public async Task<string> CreateRoom(bool isPrivate)
        {
            string sessionId = await CreateRelaySession(null);
            await CreateLobby(sessionId, Constants.kMaxPlayers, sessionId);
            return sessionId;
        }

        public async Task<string> JoinRandomRoom()
        {
            string roomId = await JoinLobby("", "");
            if(string.IsNullOrEmpty(roomId))
            {
                return await CreateRoom(false);
            }
            else
            {
                await JoinRelaySession(roomId);
                return roomId;
            }
        }
        public async Task<string> CreateRelaySession(string region)
        {
            Allocation relayAllocation = await RelayService.Instance.CreateAllocationAsync(Constants.kMaxPlayers, region);
            return await RelayService.Instance.GetJoinCodeAsync(relayAllocation.AllocationId);
        }
        public async Task JoinRelaySession(string sessionId)
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(sessionId);
        }

        public void UpdateRoomData(GameRoomData roomData)
        {

        }

        public async Task CreateLobby(string lobbyName, int maxPlayers, object additionalData)
        {
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                Data = new System.Collections.Generic.Dictionary<string, DataObject> { { Constants.kRoomId, new DataObject(DataObject.VisibilityOptions.Public, additionalData.ToString()) } }
            };
            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);
        }

        public async Task<string> JoinLobby(string lobbyId, object additionalData)
        {
            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            return lobby == null ? "" : lobby.Data[Constants.kRoomId].Value;
        }

        public Task UpdateLobbyData(string lobbyId)
        {
            throw new System.NotImplementedException();
        }
    }
}
