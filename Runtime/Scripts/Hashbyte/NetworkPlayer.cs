using System.Collections;

namespace Hashbyte.Multiplayer
{
    public class NetworkPlayer : INetworkPlayer
    {
        public string PlayerId { get; set; }
        public string PlayerName { get; set; }

        public int ActorNumber { get; set; }

        public Hashtable PlayerData { get; set; }

        public void OnTurnUpdate(bool isMyTurn)
        {
            throw new System.NotImplementedException();
        }
    }
}
