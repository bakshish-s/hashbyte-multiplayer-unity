using Unity.Services.Relay.Models;

namespace Hashbyte.Multiplayer
{
    public class UnityRoomResponse : IRoomResponse
    {
        public string RoomId { get; set; }

        public bool Success { get; set; }

        public RoomError Error { get; set; }

        public bool isHost { get; set; }
        public Allocation hostAllocation;
        public JoinAllocation clientAllocation;
    }
}
