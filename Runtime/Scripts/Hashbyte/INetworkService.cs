namespace Hashbyte.Multiplayer
{
    public interface INetworkService
    {        
        public bool ConnectToServer(IConnectSettings connectSettings);
        public void NetworkUpdate();
    }
}
