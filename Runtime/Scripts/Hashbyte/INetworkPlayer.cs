namespace Hashbyte.Multiplayer
{
    public interface INetworkPlayer
    {
        public string PlayerId { get; }
        public string PlayerName { get; }
        public int ActorNumber { get; }
        public System.Collections.Hashtable PlayerData { get; }
        public void OnTurnUpdate(bool isMyTurn);
    }
}
