using System.IO;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
namespace Hashbyte.Multiplayer
{
    public class UnityNetService : INetworkService
    {
        private NetworkDriver driver;
        private NativeList<NetworkConnection> serverConnections;
        private NetworkConnection clientConnection;
        private NetworkConnection incomingConnection;
        private NetworkEvent.Type eventType;
        private DataStreamReader dataReader;
        private bool isHost;

        public bool ConnectToServer(IConnectSettings connectSettings)
        {
            bool connectionStatus;
            if (!(connectSettings is UnityConnectSettings)) return false;
            UnityConnectSettings unityConnect = (UnityConnectSettings)connectSettings;
            isHost = unityConnect.RoomResponse.isHost;
            RelayServerData relayServerData;
            if (isHost)
                relayServerData = new RelayServerData(((UnityRoomResponse)unityConnect.RoomResponse).hostAllocation, connectSettings.ConnectionType);
            else
                relayServerData = new RelayServerData(((UnityRoomResponse)unityConnect.RoomResponse).clientAllocation, connectSettings.ConnectionType);
            CreateNetworkDriver(relayServerData);
            if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                //Failed to bind
                connectionStatus = false;
            }
            else connectionStatus = (!(isHost && driver.Listen() != 0));
            if (connectionStatus)
            {
                if (isHost) serverConnections = new NativeList<NetworkConnection>();
                else clientConnection = driver.Connect();
            }
            return connectionStatus;
        }

        public void NetworkUpdate()
        {
            BaseUpdate();
            if (isHost) HostUpdate();
            else ClientUpdate();
        }

        private void BaseUpdate()
        {
            if (!driver.IsCreated || !driver.Bound) return;
            //Keep relay server alive
            driver.ScheduleUpdate().Complete();
        }

        private void ClientUpdate()
        {
            if (clientConnection == default(NetworkConnection)) return;
            while ((eventType = clientConnection.PopEvent(driver, out dataReader)) != NetworkEvent.Type.Empty)
            {
                ParseEvent();
            }
        }

        private void HostUpdate()
        {
            if (!serverConnections.IsCreated) return;
            UpdateConnections();
            for (int i = 0; i < serverConnections.Length; i++)
            {
                if (!serverConnections.IsCreated) continue;
                while ((eventType = driver.PopEventForConnection(serverConnections[i], out dataReader)) != NetworkEvent.Type.Empty)
                {
                    ParseEvent();
                }
            }
        }

        private void CreateNetworkDriver(RelayServerData relayServerData)
        {
            NetworkSettings networkSettings = new NetworkSettings();
            networkSettings.WithRelayParameters(ref relayServerData);
            driver = NetworkDriver.Create(networkSettings);
        }

        private void UpdateConnections()
        {
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
        }

        private void ParseEvent()
        {
            switch (eventType)
            {
                // Handle Relay events.
                case NetworkEvent.Type.Data:
                    Unity.Collections.FixedString32Bytes msg = dataReader.ReadFixedString32();
                    Debug.Log($"Player received msg: {msg}");
                    ReceiveEvent(msg);
                    break;

                // Handle Connect events.
                case NetworkEvent.Type.Connect:
                    Debug.Log("Player connected to the Host");
                    break;

                // Handle Disconnect events.
                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Player got disconnected from the Host");
                    clientConnection = default(NetworkConnection);
                    break;
            }
        }

        public virtual void ReceiveEvent(FixedString32Bytes eventData)
        {
            GameEvent gameEvent = new GameEvent();
            string[] eventSplit = eventData.ToString().Split(':');
            if (eventSplit.Length > 0)
            {
                int evType = int.Parse(eventSplit[0]);
                gameEvent.eventType = (GameEventType)evType;
                if (eventSplit.Length > 1)
                {
                    gameEvent.data = eventSplit[1];
                }
            }
        }

        public void Dispose()
        {
            Debug.Log("Disposing");
            if (serverConnections.IsCreated) serverConnections.Dispose();
            if (driver.IsCreated) driver.Dispose();
            incomingConnection = default(NetworkConnection);
            clientConnection = default(NetworkConnection);
        }
    }
}
