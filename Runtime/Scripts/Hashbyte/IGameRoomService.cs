namespace Hashbyte.Multiplayer
{
    public interface IGameRoomService
    {
        /// <summary>
        /// Tries to join any available and open Rooms.    
        /// </summary>
        /// <returns>Room ID/Code/Name if joined or empty string if no room available</returns>
        public System.Threading.Tasks.Task<IRoomResponse> JoinOrCreateRoom(System.Collections.Hashtable roomProperties);
        /// <summary>
        /// Creates a new room on Network
        /// </summary>
        /// <returns>Room ID/Code/Name of the room created</returns>
        public System.Threading.Tasks.Task<IRoomResponse> CreateRoom(bool isPrivate, System.Collections.Hashtable roomProperties);
        public System.Threading.Tasks.Task<IRoomResponse> JoinRoom(string roomId, System.Collections.Hashtable options);
        public System.Threading.Tasks.Task<IRoomResponse> JoinRoomByCode(string roomCode, System.Collections.Hashtable options);
        public System.Threading.Tasks.Task<System.Collections.Generic.List<string>> FindAvailableRooms();
        public System.Threading.Tasks.Task LeaveRoom(GameRoom room);
        public System.Threading.Tasks.Task UpdateRoomProperties(string roomID, System.Collections.Hashtable roomProperties);

    }
}