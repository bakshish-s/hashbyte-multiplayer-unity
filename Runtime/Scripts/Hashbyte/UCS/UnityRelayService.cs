using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
namespace Hashbyte.Multiplayer
{
    internal class UnityRelayService
    {
        public async Task<IRoomResponse> CreateRelaySession(string region, UnityRoomResponse roomResponse)
        {
            try
            {
                Allocation relayAllocation = await RelayService.Instance.CreateAllocationAsync(Constants.kMaxPlayers, region);
                roomResponse.hostAllocation = relayAllocation;
                roomResponse.Room.RoomId = await RelayService.Instance.GetJoinCodeAsync(relayAllocation.AllocationId);
                return roomResponse;
            }
            catch (RelayServiceException exception)
            {
                Debug.Log($"Relay exception {exception.ErrorCode}, {exception.Message}");
                roomResponse = new UnityRoomResponse();
                roomResponse.Success = false;
                roomResponse.Error = new RoomError() { ErrorCode = exception.ErrorCode, Message = exception.Message, };
                return roomResponse;
            }
        }

        public async Task<IRoomResponse> JoinRelaySession(UnityRoomResponse roomResponse)
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(roomResponse.Room.RoomId);
            roomResponse.clientAllocation = allocation;
            roomResponse.Success = true;
            return roomResponse;
        }

        public void DisconnectFromRelay()
        {
            
        }
    }
}
