namespace Hashbyte.Multiplayer
{
    public interface INetworkPlayer
    {
        public string PlayerId { get; }
        public int ActorNumber { get; }
        public void OnTurnUpdate(bool isMyTurn);
    }
}
