namespace Hashbyte.Multiplayer
{
    public interface INetworkService
    {
        public bool IsConnected { get; }
        public bool IsHost { get; }
        public bool ConnectToServer(IConnectSettings connectSettings);
        public System.Threading.Tasks.Task Disconnect();
        public System.Threading.Tasks.Task RecoverConnection();
        public void NetworkUpdate();
        public void SendMove(GameEvent gameEvent);
        public void Dispose();
    }    
}
