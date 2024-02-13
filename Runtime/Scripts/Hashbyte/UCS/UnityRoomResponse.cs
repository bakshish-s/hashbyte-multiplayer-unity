using Unity.Services.Relay.Models;

namespace Hashbyte.Multiplayer
{
    public class UnityRoomResponse : IRoomResponse
    {
        public string RoomId { get; }

        public bool Success { get; }

        public RoomError Error { get; }

        public bool isHost { get; }
        public Allocation hostAllocation;
        public JoinAllocation clientAllocation;
    }
}
