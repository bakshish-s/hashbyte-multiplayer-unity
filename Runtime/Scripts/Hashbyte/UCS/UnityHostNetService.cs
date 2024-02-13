using System.Collections;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;

namespace Hashbyte.Multiplayer
{
    public class UnityHostNetService : INetworkService
    {
        private NativeList<NetworkConnection> serverConnections;
        private Allocation gameSession;
        private NetworkDriver driver;
        public bool ConnectToServer()
        {
            if (gameSession == null) return false;
            //If we are creating game session, that means we are host and we should have a list of joining players
            //Create list of joining players
            serverConnections = new NativeList<NetworkConnection>(Constants.kMaxPlayers, Allocator.Persistent);
            //The created game session will only last for 10 seconds if player does not bind with the session
            RelayServerData relayServerData = new RelayServerData(gameSession, "udp"); //Use dtls
            //Create network settings form relay data received
            NetworkSettings networkSettings = new NetworkSettings();
            networkSettings.WithRelayParameters(ref relayServerData);
            //Setup host driver (Like a socket connection)
            driver = NetworkDriver.Create(settings: networkSettings);
            //Binding the host driver with newly created game session to make it live for another 60 seconds
            if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                //Host client failed to bind                
                return false;
            }
            else
            {
                //Start listeing to game session
                if (driver.Listen() != 0)
                {                    
                    return false;
                }
                else
                {                    
                    return true;
                }
            }
        }

        public async System.Threading.Tasks.Task<string> CreateSession(string region)
        {
            //Reserve a space on Relay server 
            gameSession = await RelayService.Instance.CreateAllocationAsync(Constants.kMaxPlayers, region);
            return await RelayService.Instance.GetJoinCodeAsync(gameSession.AllocationId);            
        }

        public void JoinSession(string sessionId)
        {
            
        }

        public void NetworkUpdate()
        {
            throw new System.NotImplementedException();
        }
    }
}
