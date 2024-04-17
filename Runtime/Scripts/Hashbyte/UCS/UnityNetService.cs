using System;
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
        private CancellationTokenSource cancellationTokenSource;
        private DisconnectionHandler disconnectionHandler;
        private bool cancelConnection;
        internal UnityNetService(IMultiplayerEvents _multiplayerEvents)
        {
            multiplayerEvents = _multiplayerEvents; disconnectionHandler = new DisconnectionHandler(this);
            disconnectionHandler.OnDisconnectedFromInternet += multiplayerEvents.LostConnection;
            disconnectionHandler.OnReconnectedToInternet += multiplayerEvents.OnReconnected;
            disconnectionHandler.NoResponseFromOpponent += multiplayerEvents.OtherPlayerNotResponding;
            disconnectionHandler.OpponentReconnected += multiplayerEvents.OnOtherPlayerReconnected;
        }
        public bool ConnectToServer(IConnectSettings connectSettings)
        {
            IsHost = connectSettings.RoomResponse.Room.isHost;
            return IsHost ? ConnectAsHost() : ConnectAsClient();
        }

        private bool ConnectAsHost()
        {
            RelayServerData relayServerData = new RelayServerData(UnityRelayService.Instance.HostAllocation, Constants.kConnectionType);
            CreateNetworkDriver(relayServerData);
            if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                Debug.Log("Failed to Bind to allocation");
            }
            else if (driver.Listen() != 0)
            {
                Debug.Log("Failed to listen to server");
            }
            else
            {
                Debug.Log("Listening to server");
                serverConnections = new NativeList<NetworkConnection>(Constants.kMaxPlayers, Allocator.Persistent);
                return true;
            }
            return false;
        }

        private bool ConnectAsClient()
        {
            RelayServerData relayServerData = new RelayServerData(UnityRelayService.Instance.ClientAllocation, Constants.kConnectionType);
            CreateNetworkDriver(relayServerData);
            if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                Debug.Log("Failed to Bind to allocation");
            }
            else
            {
                Debug.Log($"Asking Host to accept my connection");
                clientConnection = driver.Connect();
                ConnectionTimeout();
                return true;
            }
            return false;
        }

        private async void ConnectionTimeout()
        {
            cancelConnection = true;
            await Task.Delay(8000);
            if (cancelConnection)
            {
                multiplayerEvents.GetTurnEventListeners().ForEach(eventListener => eventListener.OnNetworkMessage(new GameEvent() { eventType = GameEventType.CONNECT_FAILED }));
                MultiplayerService.Instance.LeaveRoom();
                Debug.Log($"Host did not accept my connection, closing connection");
            }
        }

        public async Task RejoinClientAllocation(string allocationId)
        {
            Debug.Log("Joining allocation again");
            if (await UnityRelayService.Instance.JoinRelaySession(allocationId))
            {
                Debug.Log("Allocation joined");
                ConnectAsClient();
            }
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
            //Debug.Log($"Relay connection status {driver.GetRelayConnectionStatus()}");
            //if(driver.GetRelayConnectionStatus() != RelayConnectionStatus.Established)
            //{
            //    Dispose();
            //    return;
            //}                        
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
                if (serverConnections[i] == default(NetworkConnection))
                {
                    serverConnections.RemoveAt(i);
                    --i;
                    continue;
                }
                while ((eventType = driver.PopEventForConnection(serverConnections[i], out dataReader)) != NetworkEvent.Type.Empty)
                {
                    ParseEvent();
                }
            }
        }
        public async Task RecoverConnection()
        {
            Debug.Log("Disconnecting");
            ReceiveEvent("3:Disconnecting");
            await Disconnect();
            //We might still be in lobby, if we were host let's try to create new relay allocation and give it to other player using roomperoperties
            if (IsHost)
            {
                Debug.Log("Creating new allocation now");
                ReceiveEvent("3:Creating new allocation");
                if (await UnityRelayService.Instance.CreateRelaySession())
                {
                    Debug.Log($"Allocation created");
                    ReceiveEvent("3:Allocation created");
                    ConnectAsHost();
                    MultiplayerService.Instance.UpdateRoomProperties(new System.Collections.Hashtable() { { Constants.kRoomId, UnityRelayService.Instance.JoinCode } });
                }

            }
            else
            {
                Debug.Log("Joining allocation again");
                ReceiveEvent("3:Join Allocation");
                if (await UnityRelayService.Instance.JoinRelaySession(UnityRelayService.Instance.JoinCode))
                {
                    Debug.Log("Allocation joined");
                    ReceiveEvent("3:Allocation Joined");
                    ConnectAsClient();
                }
            }
        }

        public async Task Disconnect()
        {
            if (IsHost) await HostDisconnect();
            else await ClientDisconnect();
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
            if (driver.IsCreated)
                //Make sure the disconnect event is propagated immediately
                driver.ScheduleUpdate().Complete();
            await Task.Delay(500);
            Dispose();
        }

        private async Task ClientDisconnect()
        {
            clientConnection.Close(driver);
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
                //Debug.Log($"Player joined with network ID {incomingConnection.InternalId}");
                serverConnections.Add(incomingConnection);
                multiplayerEvents.OnPlayerConnected();
                cancellationTokenSource = new CancellationTokenSource();
                disconnectionHandler.SetCancellationToken(cancellationTokenSource.Token);
            }
        }

        private void ParseEvent()
        {
            switch (eventType)
            {
                // Handle Relay events.
                case NetworkEvent.Type.Data:
                    // Debug.Log($"Size of message received {dataReader.Length}");
                    if (dataReader.Length > 32)
                    {
                        if (dataReader.Length > 64)
                        {
                            Unity.Collections.FixedString128Bytes msg = dataReader.ReadFixedString128();
                            ReceiveEvent(msg.ToString());
                        }
                        else
                        {
                            Unity.Collections.FixedString64Bytes msg = dataReader.ReadFixedString64();
                            ReceiveEvent(msg.ToString());
                        }
                    }
                    else
                    {
                        Unity.Collections.FixedString32Bytes msg = dataReader.ReadFixedString32();
                        ReceiveEvent(msg.ToString());
                    }
                    //Debug.Log($"Player received msg: {msg}");
                    break;

                // Handle Connect events.
                case NetworkEvent.Type.Connect:
                    Debug.Log("Player connected to the Host");
                    cancelConnection = false;
                    cancellationTokenSource = new CancellationTokenSource();
                    disconnectionHandler.SetCancellationToken(cancellationTokenSource.Token);
                    multiplayerEvents.OnPlayerConnected();
                    break;

                // Handle Disconnect events.
                case NetworkEvent.Type.Disconnect:
                    Unity.Networking.Transport.Error.DisconnectReason disconnectReason = (Unity.Networking.Transport.Error.DisconnectReason)dataReader.ReadByte();
                    switch (disconnectReason)
                    {
                        case Unity.Networking.Transport.Error.DisconnectReason.Default:
                            break;
                        case Unity.Networking.Transport.Error.DisconnectReason.Timeout:
                            //We were disconnected for more than 10 seconds, relay allocation timedout. Try reconnecting
                            Debug.Log($"Disconnection received. Relay allocation timeout {driver.GetRelayConnectionStatus()}");
                            break;
                        case Unity.Networking.Transport.Error.DisconnectReason.MaxConnectionAttempts:                            
                            Debug.Log($"Disconnection received. Max Attempts Received, Abandon game");
                            cancellationTokenSource?.Cancel();
                            //Abandon game ??
                            break;
                        case Unity.Networking.Transport.Error.DisconnectReason.ClosedByRemote:
                            Debug.Log($"Disconnection received. Player left intentionally");
                            if (IsHost)
                            {                                
                                serverConnections[0] = default(NetworkConnection);
                            }                            
                            cancellationTokenSource?.Cancel();
                            break;
                        case Unity.Networking.Transport.Error.DisconnectReason.Count:
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }
        public void Dispose()
        {
            Debug.Log("Disposing");
            try
            {
                if (serverConnections.IsCreated) serverConnections.Dispose();
                if (driver.IsCreated) driver.Dispose();
                incomingConnection = default(NetworkConnection);
                clientConnection = default(NetworkConnection);
                if (cancellationTokenSource != null)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource.Dispose();
                }                
            }
            catch (ObjectDisposedException e)
            {
                Debug.Log(e.Message);
            }
        }

        public virtual void ReceiveEvent(string eventData)
        {
            GameEvent gameEvent = new GameEvent();
            string[] eventSplit = eventData.Split(':');
            if (eventSplit.Length > 0)
            {
                int evType = int.Parse(eventSplit[0]);
                gameEvent.eventType = (GameEventType)evType;
                if (eventSplit.Length > 1)
                {
                    gameEvent.data = eventSplit[1];
                }
            }
            if (gameEvent.eventType == GameEventType.PING)
            {
                //Reset gamestartcount so we do not send duplicate pong
                gameStartAckCount = 0;
                multiplayerEvents.GetTurnEventListeners().ForEach(eventListener => eventListener.OnNetworkMessage(gameEvent));
                disconnectionHandler.OnPing(gameEvent.data);
                return;
            }
            else if (gameEvent.eventType == GameEventType.PONG)
            {
                //Reset gamestartcount so we do not send duplicate ping
                gameStartAckCount = 0;
                multiplayerEvents.GetTurnEventListeners().ForEach(eventListener => eventListener.OnNetworkMessage(gameEvent));
                disconnectionHandler.OnPong(gameEvent.data);
                return;
            }
            else if (gameEvent.eventType == GameEventType.PLAYER_RECONNECTED)
            {
                disconnectionHandler.OnReconnected(IsHost);
                SendMove(new GameEvent() { eventType = GameEventType.RECONNECTION_ACKNOWLEDGE });
                return;
            }
            else if (gameEvent.eventType == GameEventType.RECONNECTION_ACKNOWLEDGE)
            {
                //gameEvent.eventType = GameEventType.GAME_MOVE;
                gameEvent.data = "Reconnected To Server";
                disconnectionHandler.cancelReconnection = true;
                //multiplayerEvents.GetTurnEventListeners().ForEach(eventListener => eventListener.OnNetworkMessage(gameEvent));
                return;
            }
            else if (gameEvent.eventType == GameEventType.GAME_STARTED)
            {
                gameStartAckCount++;
                SendMove(new GameEvent() { eventType = GameEventType.GAME_ALIVE, data = (gameStartAckCount).ToString() });
                return;
            }
            else if (gameEvent.eventType == GameEventType.GAME_ALIVE)
            {
                int gameAlive = int.Parse(gameEvent.data);
                //gameAlive = 2, ack = 1 (I started late, let's start ping or pong
                //gameAlive = 2, ack = 2 (Both started at same time, host starts the game)
                //gameAlive = 1, ack = 1 (Other player started late)
                gameEvent.eventType = GameEventType.GAME_MOVE;
                if (gameAlive == 2 && gameStartAckCount == 2)
                {
                    if (IsHost)
                    {
                        Debug.Log($"First Ping sent");
                        disconnectionHandler.stopPing = false;
                        disconnectionHandler.HeartbeatClient();
                    }
                }
                else if (gameAlive == 2 && gameStartAckCount == 1)
                {
                    Debug.Log($"{(IsHost ? "Host" : "Client")} started late");
                    //I started late
                    disconnectionHandler.stopPing = false;
                    SendMove(new GameEvent() { eventType = IsHost ? GameEventType.PING : GameEventType.PONG, data = "1" });
                }
                else if(gameAlive == 1 && gameStartAckCount == 1)
                {
                    Debug.Log($"Other player started late");
                    if(IsHost)
                    {
                        Debug.Log($"First Ping sent");
                        disconnectionHandler.stopPing = false;
                        disconnectionHandler.HeartbeatClient();
                    }
                }
                return;
            }else if(gameEvent.eventType == GameEventType.GAME_ENDED)
            {
                //Stop ping until next game start
                disconnectionHandler.stopPing = true;
            }
            multiplayerEvents.GetTurnEventListeners().ForEach(eventListener => eventListener.OnNetworkMessage(gameEvent));
        }
        private int gameStartAckCount = 0;
        public void SendMove(GameEvent gameEvent)
        {
            if (gameEvent.eventType == GameEventType.GAME_STARTED)
            {
                gameStartAckCount++;
            }
            if (IsHost)
            {
                if (!serverConnections.IsCreated) return;
                foreach (NetworkConnection connection in serverConnections)
                {
                    //Debug.Log($"Sending to connection {connection} -- {connection == default(NetworkConnection)}");
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
                switch (gameEvent.size)
                {
                    case DataSize.NORMAL:
                        FixedString32Bytes msg = $"{(int)gameEvent.eventType}:{gameEvent.data}";
                        writer.WriteFixedString32(msg);
                        break;
                    case DataSize.MEDIUM:
                        FixedString64Bytes msgMedium = $"{(int)gameEvent.eventType}:{gameEvent.data}";
                        writer.WriteFixedString64(msgMedium);
                        break;
                    case DataSize.LARGE:
                        FixedString128Bytes msgLarge = $"{(int)gameEvent.eventType}:{gameEvent.data}";
                        writer.WriteFixedString128(msgLarge);
                        break;
                    default:
                        break;
                }
                int endStatus = driver.EndSend(writer);
                //Debug.Log($"Base Event Msg {msg} -- {endStatus}");
            }
            else
            {
                Debug.Log($"Send failed because of reason {statusOfSend}, Need to handle it");
            }
        }
    }
}
