namespace Hashbyte.Multiplayer
{
    public interface INetworkService
    {
        public bool IsConnected { get; }
        public bool IsHost { get; }
        public bool ConnectToServer(IConnectSettings connectSettings);
        public void Disconnect();
        public void NetworkUpdate();
        public void SendMove(GameEvent gameEvent);
        public void Dispose();
    }    
}
