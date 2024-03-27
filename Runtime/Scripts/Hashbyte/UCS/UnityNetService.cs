using System.Threading;
using System.Threading.Tasks;
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
        private bool disconnected;
        private CancellationTokenSource cancellationTokenSource;
        private CancellationToken cancellationToken;
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
            //Dispose();
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
            disconnected = false;
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
            if(driver.GetRelayConnectionStatus() == RelayConnectionStatus.AllocationInvalid)
            {
                Debug.Log("Bakshish Relay connection was detroyed. What to do now");
                return;
            }
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

        public async Task Disconnect()
        {
            if (IsHost) await HostDisconnect();
            else await ClientDisconnect();            
            //driver.Dispose();
        }

        private async Task HostDisconnect()
        {
            if (serverConnections.IsCreated)
            {
                for (int i = 0; i < serverConnections.Length; i++)
                {
                    driver.Disconnect(serverConnections[i]);
                    serverConnections[i] = default(NetworkConnection);
                }
            }
            //Make sure the disconnect event is propagated immediately
            driver.ScheduleUpdate().Complete();
            await Task.Delay(500);
            Dispose();
        }

        private async Task ClientDisconnect()
        {
            Debug.Log($"Disconnecting");
            clientConnection.Close(driver);
            Debug.Log($"Connection closed");
            driver.Disconnect(clientConnection);
            driver.ScheduleUpdate().Complete();
            Debug.Log($"Disconnected");
            clientConnection = default(NetworkConnection);
            await Task.Delay(500);
            Dispose();
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
                    Debug.Log($"Removing stale connection");
                    serverConnections.RemoveAt(i);
                    --i;
                }
            }
            while ((incomingConnection = driver.Accept()) != default(NetworkConnection))
            {
                Debug.Log($"Player joined {incomingConnection}");
                serverConnections.Add(incomingConnection);
                multiplayerEvents.OnPlayerConnected();
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;
                HeartbeatPlayer();
            }
        }
        private int pingsMissed = -1;
        private async void HeartbeatPlayer()
        {
            GameEvent pingEvent = new GameEvent() { eventType = GameEventType.PLAYER_ALIVE };
            int eventID = 1;
            Debug.Log($"Bakshish Heartbeat starting {eventID} -- {cancellationToken.IsCancellationRequested}");
            while (eventID < 100 && MultiplayerService.Instance.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                pingEvent.data = eventID.ToString();
                SendMove(pingEvent);
                Debug.Log("Ping");
                pingsMissed++;
                await Task.Delay(1000);
                eventID++;
                if (pingsMissed >= 3)
                {
                    multiplayerEvents.OtherPlayerNotResponding();
                    break;
                }
            }
            if (eventID < 100)
            {
                if (!MultiplayerService.Instance.IsConnected)
                {
                    //Send Disconnected event to player
                    multiplayerEvents.LostConnection();
                    disconnected = true;
                    TryReconnecting();
                }
            }
        }        

        private async void TryReconnecting()
        {
            int waitTime = 60/*seconds*/;
            while (waitTime > 0)
            {
                await Task.Delay(1000);
                if (MultiplayerService.Instance.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    multiplayerEvents.OnReconnected();
                    disconnected = false;
                    SendMove(new GameEvent() { eventType = GameEventType.PLAYER_RECONNECTED });
                    break;
                }
                waitTime--;
            }
            if (!disconnected)
            {
                //Resume game. Ping player for missed moves
                Debug.Log("Bakshish. Player reconnected to server now");
                HeartbeatPlayer();
            }
            else
            {
                //Was not able to reconnect. Leave game
                Debug.Log("Bakshish. Player failed to reconnect in 60 seconds to server");
            }
        }

        private void ParseEvent()
        {
            switch (eventType)
            {
                // Handle Relay events.
                case NetworkEvent.Type.Data:
                    Unity.Collections.FixedString32Bytes msg = dataReader.ReadFixedString32();
                    //Debug.Log($"Player received msg: {msg}");
                    ReceiveEvent(msg);
                    break;

                // Handle Connect events.
                case NetworkEvent.Type.Connect:
                    Debug.Log("Player connected to the Host");
                    cancellationTokenSource = new CancellationTokenSource();
                    cancellationToken = cancellationTokenSource.Token;
                    multiplayerEvents.OnPlayerConnected();
                    HeartbeatPlayer();
                    break;

                // Handle Disconnect events.
                case NetworkEvent.Type.Disconnect:
                    var disconnectReason = dataReader.ReadByte();
                    if ((Unity.Networking.Transport.Error.DisconnectReason)disconnectReason == Unity.Networking.Transport.Error.DisconnectReason.ClosedByRemote)
                    {
                        Debug.Log($"Disconnection received {disconnectReason} Player left intentionally");
                    } 
                    cancellationTokenSource.Cancel();
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
            cancellationTokenSource.Cancel();
            cancellationTokenSource.Dispose();
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
            if (gameEvent.eventType == GameEventType.PLAYER_ALIVE && MultiplayerService.Instance.IsConnected)
            {
                gameEvent.eventType = GameEventType.PLAYER_ALIVE_RESPONSE;
                Debug.Log("Ping Recieved, Returning");
                //Acknowledge other player
                SendMove(gameEvent);
                return;
            }
            else if (gameEvent.eventType == GameEventType.PLAYER_ALIVE_RESPONSE && MultiplayerService.Instance.IsConnected)
            {
                pingsMissed--;
                return;
            }else if(gameEvent.eventType == GameEventType.PLAYER_RECONNECTED && MultiplayerService.Instance.IsConnected)
            {
                multiplayerEvents.OnOtherPlayerReconnected();
                pingsMissed = 0;                
                HeartbeatPlayer();
                return;
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
            int statusOfSend = driver.BeginSend(connection, out var writer);
            if (statusOfSend == 0)
            {
                FixedString32Bytes msg = $"{(int)gameEvent.eventType}:{gameEvent.data}";
                // Send the message. Aside from FixedString32, many different types can be used.
                writer.WriteFixedString32(msg);
                int endStatus = driver.EndSend(writer);
                //Debug.Log($"Base Event Msg {msg} -- {endStatus}");
            }
            else
            {
                //Debug.Log($"Send failed because of reason {statusOfSend}");
            }
        }
    }
}
