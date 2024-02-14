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
        private NetworkDriver driver;
        public bool ConnectToServer(IConnectSettings connectSettings)
        {
            if (connectSettings == null || !(connectSettings is UnityConnectSettings)) return false;
            UnityConnectSettings unityConnect = connectSettings as UnityConnectSettings;
            if(unityConnect.roomResponse == null) return false;
            //If we are creating game session, that means we are host and we should have a list of joining players
            //Create list of joining players
            serverConnections = new NativeList<NetworkConnection>(Constants.kMaxPlayers, Allocator.Persistent);
            //The created game session will only last for 10 seconds if player does not bind with the session
            RelayServerData relayServerData;
            if (unityConnect.roomResponse.isHost)
                relayServerData = new RelayServerData(unityConnect.roomResponse.hostAllocation, unityConnect.ConnectionType); //Use dtls
            else
                relayServerData = new RelayServerData(unityConnect.roomResponse.clientAllocation, unityConnect.ConnectionType);
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

        public void NetworkUpdate()
        {
            throw new System.NotImplementedException();
        }
    }
}
