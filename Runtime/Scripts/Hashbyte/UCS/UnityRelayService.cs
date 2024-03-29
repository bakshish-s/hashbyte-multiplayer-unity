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
        public Allocation HostAllocation{get; private set;}
        public JoinAllocation ClientAllocation { get; private set; }
        public string JoinCode{get; private set;}
        public async Task<bool> CreateRelaySession()
        {
            try
            {
                HostAllocation = await RelayService.Instance.CreateAllocationAsync(Constants.kMaxPlayers, Constants.kRegionForServer);
                JoinCode = await RelayService.Instance.GetJoinCodeAsync(HostAllocation.AllocationId);
                return true;
            }
            catch (RelayServiceException exception)
            {
                Debug.Log($"Relay exception {exception.ErrorCode}, {exception.Message}");
                return false;
            }
        }

        public async Task<bool> JoinRelaySession(string joinCode)
        {
            try
            {
                JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
                JoinCode = joinCode;
                ClientAllocation = allocation;
                return true;
            }catch (RelayServiceException exception)
            {
                Debug.Log($"Relay Exception while joining {exception.ErrorCode}, {exception.Message}");
                return false;
            }
        }

        //public async Task<IRoomResponse> JoinRelaySession(UnityRoomResponse roomResponse)
        //{
        //    try
        //    {
        //        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(roomResponse.Room.RoomId);
        //        roomResponse.clientAllocation = allocation;
        //        roomResponse.Success = true;
        //        return roomResponse;
        //    }
        //    catch (RelayServiceException exception)
        //    {
        //        Debug.Log($"Relay exception {exception.ErrorCode}, {exception.Message}");
        //        roomResponse = new UnityRoomResponse();
        //        roomResponse.Success = false;
        //        roomResponse.Error = new RoomError() { ErrorCode = exception.ErrorCode, Message = exception.Message, };
        //        return roomResponse;
        //    }
        //}

        public void DisconnectFromRelay()
        {

        }
    }
}
