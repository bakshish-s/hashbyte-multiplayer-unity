using System.Collections.Generic;

namespace Hashbyte.Multiplayer
{
    public interface IRoomResponse
    {
        public bool Success { get; }
        public RoomError Error { get; }
        public GameRoom Room { get; } 
    }
    public struct RoomError
    {
        public int ErrorCode;
        public string Message;
    }

    public class GameRoom
    {
        public string RoomId { get; internal set; }
        public string LobbyId { get; internal set; }
        public bool isHost { get; internal set; }
        public List<INetworkPlayer> players;
        public System.Collections.Generic.List<string> Players { get; internal set; }
        public System.Collections.Hashtable RoomOptions { get; internal set; }
        public string LobbyCode { get; internal set; }

        public GameRoom()
        {

        }
        public GameRoom(string roomId, string lobbyId, bool isHost, System.Collections.Hashtable options)
        {
            RoomId = roomId;
            LobbyId = lobbyId;
            this.isHost = isHost;
            RoomOptions = options;
            string[] playerNames = options[Constants.kPlayers].ToString().Split(':');
            Players = new System.Collections.Generic.List<string>(playerNames);
        }

        public void AddPlayer(string playerId) { 
            Players.Add(playerId);
        }

        public void InsertPlayer(string playerId, int index) {
            Players.Insert(index, playerId);
        }
    }
}