using System.Collections.Generic;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
namespace Hashbyte.Multiplayer
{
    public class UnityNetService : INetworkService
    {
        public bool IsHost { get; private set; }
        public bool IsConnected { get; private set; }
        private NetworkDriver driver;
        private NativeList<NetworkConnection> serverConnections;
        private NetworkConnection clientConnection;
        private NetworkConnection incomingConnection;
        private NetworkEvent.Type eventType;
        private DataStreamReader dataReader;
        private IMultiplayerEvents multiplayerEvents;
        internal UnityNetService(IMultiplayerEvents _multiplayerEvents) { multiplayerEvents = _multiplayerEvents; }
        public bool ConnectToServer(IConnectSettings connectSettings)
        {            
            bool connectionStatus;
            if (!(connectSettings is UnityConnectSettings)) return false;
            UnityConnectSettings unityConnect = (UnityConnectSettings)connectSettings;
            IsHost = unityConnect.RoomResponse.Room.isHost;
            RelayServerData relayServerData;
            if (IsHost)
                relayServerData = new RelayServerData(((UnityRoomResponse)unityConnect.RoomResponse).hostAllocation, connectSettings.ConnectionType);
            else
                relayServerData = new RelayServerData(((UnityRoomResponse)unityConnect.RoomResponse).clientAllocation, connectSettings.ConnectionType);
            Dispose();
            CreateNetworkDriver(relayServerData);
            if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                //Failed to bind
                connectionStatus = false;
            }
            else connectionStatus = (!(IsHost && driver.Listen() != 0));
            if (connectionStatus)
            {
                if (IsHost) serverConnections = new NativeList<NetworkConnection>(Constants.kMaxPlayers, Allocator.Persistent);
                //else clientConnection = driver.Connect();
            }
            if (!IsHost)
            {
                Debug.Log($"Asking Host to accept my connection {((UnityRoomResponse)unityConnect.RoomResponse).clientAllocation}");
                clientConnection = driver.Connect();
            }
            Debug.Log($"Ready to start game ");
            IsConnected = connectionStatus;
            return connectionStatus;
        }

        public void NetworkUpdate()
        {
            BaseUpdate();
            if (IsHost) HostUpdate();
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

        public void Disconnect()
        {
            if (IsHost) HostDisconnect();
            else ClientDisconnect();
            //driver.Dispose();
        }

        private void HostDisconnect()
        {
            if(serverConnections.IsCreated)
            {
                for(int i=0; i<serverConnections.Length; i++)
                {
                    driver.Disconnect(serverConnections[i]);
                    serverConnections[i] = default(NetworkConnection);
                }
            }            
        }

        private void ClientDisconnect()
        {
            Debug.Log($"Disconnecting");
            clientConnection.Close(driver);
            Debug.Log($"Connection closed");
            driver.Disconnect(clientConnection);
            Debug.Log($"Disconnected");
            clientConnection = default(NetworkConnection);
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
                multiplayerEvents.OnPlayerConnected();
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
                    multiplayerEvents.OnPlayerConnected();
                    break;

                // Handle Disconnect events.
                case NetworkEvent.Type.Disconnect:
                    Debug.Log($"Disconnection received {dataReader}");
                    //clientConnection = default(NetworkConnection);
                    break;
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
            multiplayerEvents.GetTurnEventListeners().ForEach(eventListener => eventListener.OnNetworkMessage(gameEvent));
        }

        public void SendMove(GameEvent gameEvent)
        {
            if (IsHost)
            {
                foreach (NetworkConnection connection in serverConnections)
                {
                    SendMoveToConnection(gameEvent, connection);
                }
            }
            else
            {
                SendMoveToConnection(gameEvent, clientConnection);
            }
        }

        private void SendMoveToConnection(GameEvent gameEvent, NetworkConnection connection)
        {
            if (driver.BeginSend(connection, out var writer) == 0)
            {
                FixedString32Bytes msg = $"{(int)gameEvent.eventType}:{gameEvent.data}";
                // Send the message. Aside from FixedString32, many different types can be used.
                writer.WriteFixedString32(msg);
                Debug.Log($"Base Event Msg {msg}");
                driver.EndSend(writer);
            }
        }
    }
}
