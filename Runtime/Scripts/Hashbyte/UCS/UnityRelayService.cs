using System;
using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
namespace Hashbyte.Multiplayer
{
    internal class UnityRelayService
    {
        #region Singleton
        private static UnityRelayService instance;
        public static UnityRelayService Instance
        {
            get
            {
                if (instance == null) instance = new UnityRelayService();
                return instance;
            }
        }
        private UnityRelayService() { }
        #endregion        
        public async Task<(Allocation, string)> CreateRelaySession()
        {
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(Constants.kMaxPlayers, Constants.kRegionForServer);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                return (allocation, joinCode);
            }
            catch (RelayServiceException exception)
            {
                Debug.Log($"Relay exception {exception.ErrorCode}, {exception.Message}");
                return (null, exception.Message);
            }
        }

        public async Task<IRoomResponse> JoinRelaySession(UnityRoomResponse roomResponse)
        {
            try
            {
                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(roomResponse.Room.RoomId);
                roomResponse.clientAllocation = allocation;
                roomResponse.Success = true;
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

        public void DisconnectFromRelay()
        {

        }
    }
}
