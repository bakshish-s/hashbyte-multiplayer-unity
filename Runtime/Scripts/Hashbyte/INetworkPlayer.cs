public interface INetworkPlayer
{
    public string PlayerId { get; }
    public int actorNumber { get; }
    public void OnTurnUpdate(bool isMyTurn);
}
