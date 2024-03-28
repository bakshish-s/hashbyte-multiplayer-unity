using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Core;
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
        private string roomId;
        internal UnityNetService(IMultiplayerEvents _multiplayerEvents) { multiplayerEvents = _multiplayerEvents; }
        public bool ConnectToServer(IConnectSettings connectSettings)
        {
            bool connectionStatus;
            if (!(connectSettings is UnityConnectSettings)) return false;
            UnityConnectSettings unityConnect = (UnityConnectSettings)connectSettings;
            IsHost = unityConnect.RoomResponse.Room.isHost;
            roomId = unityConnect.RoomResponse.Room.RoomId;
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

        public async Task RejoinClientAllocation(string allocationId)
        {
            Unity.Services.Relay.Models.JoinAllocation allocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(allocationId);
            RelayServerData relayServerData = new RelayServerData(allocation, Constants.kConnectionType);
            CreateNetworkDriver(relayServerData);
            if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                Debug.Log("Failed to bind");
            }
            else
            {
                clientConnection = driver.Connect();
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
                while ((eventType = driver.PopEventForConnection(serverConnections[i], out dataReader)) != NetworkEvent.Type.Empty)
                {
                    ParseEvent();
                }
            }
        }
        float timeForNextPing = 5;
        bool pongReceived;
        GameEvent ping = new GameEvent() { eventType = GameEventType.PING };
        private async void CheckPingHost()
        {
            //Wait one second before sending next ping
            await Task.Delay(1000);
            pongReceived = false;
            timeForNextPing = 20;
            DateTime startTime = DateTime.Now;
            SendMove(ping);
            GameEvent gameEvent = new GameEvent() { eventType = GameEventType.GAME_ALIVE };
            while (!pongReceived && timeForNextPing > 0)
            {
                await Task.Yield();
                multiplayerEvents.GetTurnEventListeners().ForEach(eventListener => eventListener.OnNetworkMessage(gameEvent));
                if (!MultiplayerService.Instance.IsConnected)
                {
                    //I lost connection to internet. Immediately tell user
                    multiplayerEvents.LostConnection();
                    TryReconnecting();
                    break;
                }
                timeForNextPing = (float)(20 - (DateTime.Now - startTime).TotalSeconds);
            }
            if (!pongReceived && MultiplayerService.Instance.IsConnected)
            {
                multiplayerEvents.OtherPlayerNotResponding();
            }
        }
        bool pingReceived;
        private async void CheckPingClient()
        {
            await Task.Delay(900);
            pingReceived = false;
            timeForNextPing = 5;
            DateTime startTime = DateTime.Now;
            while (!pingReceived && timeForNextPing > 0)
            {
                await Task.Yield();
                if (!MultiplayerService.Instance.IsConnected)
                {
                    //I lost connection to internet. Immediately tell user
                    Debug.Log("Bakshish. Lost Connection --- ");
                    multiplayerEvents.LostConnection();
                    TryReconnecting();
                    break;
                }
                timeForNextPing = (float)(5 - (DateTime.Now - startTime).TotalSeconds);
            }
            if (!pingReceived && MultiplayerService.Instance.IsConnected)
            {
                multiplayerEvents.OtherPlayerNotResponding();
            }
        }
        private async void DisconnectAndRecover()
        {
            Debug.Log("Disconnecting");
            await Disconnect();
            //We might still be in lobby, if we were host let's try to create new relay allocation and give it to other player using roomperoperties
            if (IsHost)
            {
                Debug.Log("Creating new allocation now");
                Unity.Services.Relay.Models.Allocation allocation = await Unity.Services.Relay.RelayService.Instance.CreateAllocationAsync(Constants.kMaxPlayers, Constants.kRegionForServer);
                Debug.Log($"Allocation created {allocation.AllocationId}");
                string roomId = await Unity.Services.Relay.RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
                Debug.Log($"New Room ID generated {roomId}");
                //Should create new network driver here as well
                RelayServerData relayServerData = new RelayServerData(allocation, Constants.kConnectionType);
                CreateNetworkDriver(relayServerData);
                if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
                {
                    Debug.Log($"Binding failed");
                }
                else
                {
                    if(driver.Listen() != 0)
                    {
                        Debug.Log("Listening failed");
                    }
                    else
                    {
                        serverConnections = new NativeList<NetworkConnection>(Constants.kMaxPlayers, Allocator.Persistent);
                    }
                }
                MultiplayerService.Instance.UpdateRoomProperties(new System.Collections.Hashtable() { { Constants.kRoomId, roomId } });
            }
            else
            {
                Debug.Log("Joining allocation again");
                Unity.Services.Relay.Models.JoinAllocation allocation = await Unity.Services.Relay.RelayService.Instance.JoinAllocationAsync(roomId);
                Debug.Log("Allocation joined");
                RelayServerData relayServerData = new RelayServerData(allocation, Constants.kConnectionType);
                CreateNetworkDriver(relayServerData);
                if(driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
                {
                    Debug.Log($"Binding failed");
                }
                else
                {
                    Debug.Log("Binding succesfull, asking host to accept connection");
                    clientConnection = driver.Connect();
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
            if (driver.IsCreated)
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
                Debug.Log($"Player joined {incomingConnection.InternalId}");
                serverConnections.Add(incomingConnection);
                multiplayerEvents.OnPlayerConnected();
                cancellationTokenSource = new CancellationTokenSource();
                cancellationToken = cancellationTokenSource.Token;
                CheckPingHost();
            }
        }
        private int pingsMissed = -1;
        private async void HeartbeatPlayer()
        {
            GameEvent pingEvent = new GameEvent() { eventType = GameEventType.PING };
            int eventID = 1;
            Debug.Log($"Bakshish Heartbeat starting {eventID} -- {cancellationToken.IsCancellationRequested}");
            while (eventID < 100 && MultiplayerService.Instance.IsConnected && !cancellationToken.IsCancellationRequested)
            {
                pingEvent.data = eventID.ToString();
                SendMove(pingEvent);
                //Debug.Log("Ping");
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
            float waitTime = 60/*seconds*/;
            Debug.Log("Bakshish. Trying to reconnect");
            while (waitTime > 0)
            {
                await Task.Delay(200);
                if (MultiplayerService.Instance.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    multiplayerEvents.OnReconnected();
                    disconnected = false;
                    SendMove(new GameEvent() { eventType = GameEventType.PLAYER_RECONNECTED });
                    break;
                }
                waitTime -= 0.2f;
            }
            if (!disconnected)
            {
                //Resume game. Ping player for missed moves
                Debug.Log("Bakshish. Reconnected to server. Handle Relay issue here");
                DisconnectAndRecover();
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
                    CheckPingClient();
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
                            if (!IsHost)
                            {
                                //if (driver.Bind(NetworkEndPoint.AnyIpv4) != 0)
                                //{
                                //    Debug.Log($"Failed to bind");
                                //}
                                //else
                                //{
                                //    Debug.Log("Binding Done");
                                //    if (!IsHost)
                                //    {
                                //        clientConnection = driver.Connect();
                                //        Debug.Log($"Requested host for connection");
                                //    }
                                //}
                                //DisconnectAndRecover();
                            }
                            //DisconnectAndRecover();
                            //Restart whole process from starting
                            break;
                        case Unity.Networking.Transport.Error.DisconnectReason.MaxConnectionAttempts:
                            break;
                        case Unity.Networking.Transport.Error.DisconnectReason.ClosedByRemote:
                            Debug.Log($"Disconnection received. Player left intentionally");
                            break;
                        case Unity.Networking.Transport.Error.DisconnectReason.Count:
                            break;
                        default:
                            break;
                    }
                    cancellationTokenSource?.Cancel();
                    //clientConnection = default(NetworkConnection);
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
            if (gameEvent.eventType == GameEventType.PING)
            {
                //Host sent us a ping, to confirm we are alive, send pong back
                multiplayerEvents.GetTurnEventListeners().ForEach(eventListener => eventListener.OnNetworkMessage(gameEvent));
                gameEvent.eventType = GameEventType.PONG;
                pingReceived = true;
                SendMove(gameEvent);
                CheckPingClient();
                return;
            }
            else if (gameEvent.eventType == GameEventType.PONG)
            {
                //Other player confirmed connected by sending pong back
                pongReceived = true;
                multiplayerEvents.GetTurnEventListeners().ForEach(eventListener => eventListener.OnNetworkMessage(gameEvent));
                //Resend ping to player to keep connection alive
                CheckPingHost();
                return;
            }
            else if (gameEvent.eventType == GameEventType.PLAYER_RECONNECTED)
            {
                multiplayerEvents.OnOtherPlayerReconnected();
                //Other player reconnected, we establish ping again
                if (IsHost) CheckPingHost();
                else
                {
                    gameEvent.eventType = GameEventType.PONG;
                    pingReceived = true;
                    SendMove(gameEvent);
                }
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
