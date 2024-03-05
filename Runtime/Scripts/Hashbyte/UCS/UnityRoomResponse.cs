using System.Collections;
using Unity.Services.Relay.Models;

namespace Hashbyte.Multiplayer
{
    public class UnityRoomResponse : IRoomResponse
    {
        public bool Success { get; set; }
        public RoomError Error { get; set; }        
        public GameRoom Room { get; set; }

        public Allocation hostAllocation;
        public JoinAllocation clientAllocation;
    }
}
