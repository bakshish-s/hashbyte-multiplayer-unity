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
        public bool isPrivateRoom { get; internal set; }
        public System.Collections.Generic.Dictionary<int, INetworkPlayer> players;        
        public System.Collections.Hashtable RoomOptions { get; internal set; }
        public string LobbyCode { get; internal set; }
        public string RoomCode => LobbyCode;        

        public GameRoom()
        {
            players = new System.Collections.Generic.Dictionary<int, INetworkPlayer>();
            RoomOptions = new System.Collections.Hashtable();
        }
        public GameRoom(string roomId, string lobbyId, bool isHost, System.Collections.Hashtable options)
        {
            RoomId = roomId;
            LobbyId = lobbyId;
            this.isHost = isHost;
            RoomOptions = options;
            string[] playerNames = options[Constants.kPlayers].ToString().Split(':');
        }
        public void AddPlayer(INetworkPlayer player)
        {
            players.Add(player.ActorNumber, player);
        }
        public INetworkPlayer RemovePlayer(int actorNumber)
        {
            INetworkPlayer playerToRemove = players[actorNumber];
            players.Remove(actorNumber);
            return playerToRemove;
        }
    }
}