using System.Threading.Tasks;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
namespace Hashbyte.Multiplayer
{
    public class ClientNetwork : GBRelayNetwork
    {
        private NetworkConnection clientConnection;
        public ClientNetwork(IRelayEvents relayEventListener) : base(relayEventListener)
        {
            clientConnection = default(NetworkConnection);
        }

        public async Task JoinGameSession(string gameSessionID)
        {
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(gameSessionID);
            relayEventListener?.OnGameSessionJoined();
            RelayServerData relayServerData = new RelayServerData(allocation, "udp");//Use dtls
                                                                                     //Create network settings form relay data received
            NetworkSettings networkSettings = new NetworkSettings();
            networkSettings.WithRelayParameters(ref relayServerData);
            // Setup client driver(Like a socket connection)
            driver = NetworkDriver.Create(settings: networkSettings);
            //Binding the driver to created game session
            if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                //client failed to bind
                relayEventListener?.OnNetworkError(NetworkErrorCode.BINDING_FAILED, "Host client failed to bind");
            }
            else
            {
                //Binding succesfull. Connected to game session
                relayEventListener?.OnConnectedToServer();
            }
            await Task.Delay(1000);
            Debug.Log("Asking Host to accept my connection");
            clientConnection = driver.Connect();
            Debug.Log("Requested Host to accept my connection " + clientConnection);
        }

        public override void NetworkUpdate()
        {
            base.NetworkUpdate();
            // Resolve event queue.
            NetworkEvent.Type eventType;
            if (clientConnection == default(NetworkConnection)) return;
            while ((eventType = clientConnection.PopEvent(driver, out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    // Handle Relay events.
                    case NetworkEvent.Type.Data:
                        Unity.Collections.FixedString32Bytes msg = stream.ReadFixedString32();
                        Debug.Log($"Player received msg: {msg}");
                        ReceiveEvent(msg);
                        break;

                    // Handle Connect events.
                    case NetworkEvent.Type.Connect:
                        Debug.Log("Player connected to the Host");
                        relayEventListener?.OnConnectedToHost();
                        break;

                    // Handle Disconnect events.
                    case NetworkEvent.Type.Disconnect:
                        Debug.Log("Player got disconnected from the Host");
                        clientConnection = default(NetworkConnection);
                        break;
                }
            }
        }

        public override void SendEvent(GameEvent gameEvent)
        {
            Debug.Log($"Trying to send event {gameEvent.eventType}");
            if (!clientConnection.IsCreated)
            {
                //Not connected to host network yet
                return;
            }
            SendGameEvent(clientConnection, gameEvent);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Debug.Log("Disposing");
            clientConnection = default(NetworkConnection);
        }
    }
}