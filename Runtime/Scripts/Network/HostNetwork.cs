using System.Threading.Tasks;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
namespace Hashbyte.Multiplayer
{
    public class HostNetwork : GBRelayNetwork
    {        
        private NativeList<NetworkConnection> serverConnections;
        private NetworkConnection incomingConnection;
        private DataStreamReader dataReader;

        public HostNetwork(IRelayEvents relayEventListener) : base(relayEventListener)
        {
        }
        //Frank:Host network interface to separate out methods 
        public async Task<string> CreateGameSession(int maxPlayers = Constants.kMaxPlayers, string region = null)
        {
            //Creating game session on Relay Server
            gameSession = await RelayService.Instance.CreateAllocationAsync(maxPlayers, region);
            string sessionId = await RelayService.Instance.GetJoinCodeAsync(gameSession.AllocationId);
            relayEventListener?.OnGameSessionCreated(sessionId);
            //If we are creating game session, that means we are host and we should have a list of joining players
            //Create list of joining players
            serverConnections = new NativeList<NetworkConnection>(maxPlayers, Allocator.Persistent);
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
                relayEventListener?.OnNetworkError(NetworkErrorCode.BINDING_FAILED, "Host client failed to bind");
                return Constants.kInvalidCode;
            }
            else
            {
                //Start listeing to game session
                if (driver.Listen() != 0)
                {
                    //Host client failed to listen
                    relayEventListener?.OnNetworkError(NetworkErrorCode.LISTENING_FAILED, "Host client failed to listen");
                    return Constants.kInvalidCode;
                }
                else
                {
                    //Binding succesfull. Game Session Created
                    relayEventListener?.OnConnectedToServer();
                    return sessionId;
                }
            }
        }

        public override void NetworkUpdate()
        {
            base.NetworkUpdate();
            if (!serverConnections.IsCreated) return;
            //Clean up any stale connections
            for (int i = 0; i < serverConnections.Length; i++)
            {
                if (!serverConnections[i].IsCreated)
                {
                    serverConnections.RemoveAt(i);
                    --i;
                }
            }            
            while ((incomingConnection = driver.Accept()) != default(NetworkConnection))
            {
                Debug.Log($"Player joined {incomingConnection}");
                serverConnections.Add(incomingConnection);
            }

            //Iterate through all accepted network connections
            for (int i = 0; i < serverConnections.Length; i++)
            {
                if (!serverConnections.IsCreated) continue;
                NetworkEvent.Type eventType;
                while ((eventType = driver.PopEventForConnection(serverConnections[i], out dataReader)) != NetworkEvent.Type.Empty)
                {
                    switch (eventType)
                    {
                        case NetworkEvent.Type.Data:
                            FixedString32Bytes msg = dataReader.ReadFixedString32();
                            Debug.Log($"Player received msg: {msg}");
                            ReceiveEvent(msg);
                            break;
                        case NetworkEvent.Type.Disconnect:
                            //Send disconnect event of a client
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public override void SendEvent(GameEvent gameEvent)
        {
            Debug.Log($"Sending event to {serverConnections.Length}");
            if (serverConnections.Length == 0)
            {
                //No players joined to send message
                return;
            }            
            //Send event to all connected clients
            for (int i = 0; i < serverConnections.Length; i++)
            {
                SendGameEvent(serverConnections[i], gameEvent);
            }
        }        

        public override void OnDestroy()
        {
            Debug.Log("Disposing");
            base.OnDestroy();
            incomingConnection = default(NetworkConnection);
            serverConnections.Dispose();
        }
    }
}
