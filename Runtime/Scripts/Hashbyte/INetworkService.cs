namespace Hashbyte.Multiplayer
{
    public interface INetworkService
    {
        public System.Threading.Tasks.Task<string> CreateSession(string region);
        public void JoinSession(string sessionId);
        public bool ConnectToServer();
        public void NetworkUpdate();
    }
}
