namespace Hashbyte.Multiplayer
{
    public interface IGameRoomService
    {
        /// <summary>
        /// Tries to join any available and open Rooms.    
        /// </summary>
        /// <returns>Room ID/Code/Name if joined or empty string if no room available</returns>
        public System.Threading.Tasks.Task<IRoomResponse> JoinRandomRoom();
        /// <summary>
        /// Creates a new room on Network
        /// </summary>
        /// <returns>Room ID/Code/Name of the room created</returns>
        public System.Threading.Tasks.Task<IRoomResponse> CreateRoom(bool isPrivate);
        public System.Threading.Tasks.Task UpdateRoomProperties(string roomID, System.Collections.Hashtable roomProperties);
        public void RegisterCallbacks(INetworkEvents networkEvents); 
    }
}