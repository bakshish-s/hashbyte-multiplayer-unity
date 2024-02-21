namespace Hashbyte.Multiplayer
{
    public class NetworkPlayer : INetworkPlayer
    {
        public string PlayerId { get; set; }

        public int actorNumber => throw new System.NotImplementedException();

        public void OnTurnUpdate(bool isMyTurn)
        {
            throw new System.NotImplementedException();
        }
    }
}
