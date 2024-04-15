using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    internal class DisconnectionHandler
    {
        private float waitTime;
        private bool pingReceived, pongReceived;
        private GameEvent ping = new GameEvent() { eventType = GameEventType.PING };
        private GameEvent pong = new GameEvent() { eventType = GameEventType.PONG };
        private INetworkService network;
        private CancellationToken cancellationToken;
        private const float timeBetweenPings = 2;
        private const float maxTimeWithoutPing = 2;

        #region Ping paramteres 
        private DateTime startTime;
        private CancellationTokenSource pingTask;
        private int pingId;
        private int pingReceivedId;
        private int pongReceivedId;

        #endregion

        public delegate void DisconnectionEvents();
        public event DisconnectionEvents OnDisconnectedFromInternet, NoResponseFromOpponent, OpponentReconnected, OnReconnectedToInternet;

        public DisconnectionHandler(INetworkService _network)
        {
            network = _network;
        }
        #region Host Ping System
        public async void HeartbeatClient()
        {
            //Wait half a second for things to reset before sending next heartbeat
            //int randomDelay = UnityEngine.Random.Range(800, 7000);
            //await Task.Delay(randomDelay);
            pingId++;
            if (pingTask == null || pingTask.IsCancellationRequested)
            {
                pingTask = new CancellationTokenSource();
            }
            pongReceived = false;
            int pingCount = 1;
            while (pingCount <= 3 && !pongReceived)
            {
                //Ping client and wait for response
                ping.data = pingId.ToString();
                //Debug.Log("Ping sent to client");
                bool clientResponded = await PingClient();
                if (clientResponded)
                {
                    //Debug.Log($"Client responded, exiting");
                    break;
                }
                else
                {
                    pingCount++;
                    Debug.Log($"Client not responded in 2 seconds, checking internet");
                    if (!await CheckInternet()) break;
                    //Our internet is connected, try reaching client again
                    Debug.Log($"Our internet is connected, sending ping again {pingCount}");
                }
            }
            //Client really not connected to internet
            if (pingCount > 3)
            {
                NoResponseFromOpponent?.Invoke();
            }
        }

        private async Task<bool> PingClient()
        {
            startTime = DateTime.Now;
            waitTime = timeBetweenPings;
            network.SendMove(ping);
            while (!pongReceived && waitTime > 0 && !pingTask.IsCancellationRequested)
            {
                await Task.Yield();
                waitTime = (float)(timeBetweenPings - (DateTime.Now - startTime).TotalSeconds);
            }
            //Check if client responded
            if (!pingTask.IsCancellationRequested)
            {
                if (pongReceived)
                {
                    return true;
                }
            }
            return false;
        }
        public void OnPong(string data)
        {

            //Other player confirmed connected by sending pong back
            int pongId = int.Parse(data);
            if (pongId > pongReceivedId)
            {
                pongReceivedId = pongId;
                pongReceived = true;
                //Debug.Log($"Pong: {data}");
                //pingTask.Cancel();
                //Resend ping to player to keep connection alive
                HeartbeatClient();
            }
        }
        #endregion

        #region Client Ping System
        public async void CheckPing()
        {
            await Task.Delay(200);
            pingReceived = false;
            if (pingTask == null || pingTask.IsCancellationRequested)
            {
                pingTask = new CancellationTokenSource();
            }
            int waitCount = 1;
            while (waitCount <= 3 && !pingTask.IsCancellationRequested)
            {
                startTime = DateTime.Now;
                waitTime = maxTimeWithoutPing;
                while (!pingReceived && waitTime > 0 && !pingTask.IsCancellationRequested)
                {
                    await Task.Yield();
                    waitTime = (float)(maxTimeWithoutPing - (DateTime.Now - startTime).TotalSeconds);
                }
                if (pingReceived)
                {
                    break;
                }
                else
                {
                    //Check internet connection
                    Debug.Log($"Ping not received in 2 seconds {waitCount}");
                    waitCount++;
                    if (!await CheckInternet()) break;
                    Debug.Log($"Our internet is connected, sending ping again {pingId}");
                }
            }
            if (waitCount > 3)
            {
                NoResponseFromOpponent?.Invoke();
            }
        }


        public void OnPing(string data)
        {
            //Host sent us a ping, to confirm we are alive, send pong back
            pingReceived = true;
            int pingId = int.Parse(data);
            //Since same ping is sent three times in interval of 2 seconds, chances of packet loss are almost 0 if internet is connected
            if (pingId > pingReceivedId)
            {
                pingReceivedId = pingId;
                //int randomDelay = UnityEngine.Random.Range(800, 7000);
                //Debug.Log($"Will respond to ping {pingId} in {randomDelay}ms");
                //await Task.Delay(randomDelay);
                //Debug.Log($"Ping received, sending Pong {data}");
                pong.data = pingReceivedId.ToString();
                network.SendMove(pong);
                CheckPing();
            }
            //if(pingTask != null) pingTask.Cancel();

        }
        #endregion

        #region Common
        private async Task<bool> CheckInternet()
        {
            if (!await MultiplayerService.Instance.internetUtility.IsConnectedToInternet(pingTask.Token))
            {
                //Either internet is not connected or pong received
                if (pingTask.IsCancellationRequested)
                {
                    //We don't need to recover anything, we need to get out from here
                    pingTask.Dispose();
                    return false;
                }
                else
                {
                    //Internet is not connected
                    OnDisconnectedFromInternet?.Invoke();
                    TryReconnecting();
                    pingTask.Dispose();
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
        #endregion



        public void OnReconnected(bool isHost)
        {
            OpponentReconnected?.Invoke();
            //Other player reconnected, we establish ping again
            if (isHost) HeartbeatClient();
            else
            {
                pingReceived = true;
                pong.data = pingReceivedId.ToString();
                network.SendMove(pong);
            }
        }

        private async void TryReconnecting()
        {
            float waitTime = 60/*seconds*/;
            Debug.Log("Bakshish. Trying to reconnect");
            //((UnityNetService)network).ReceiveEvent("3:Reconnecting");
            bool disconnected = true;
            while (waitTime > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    //((UnityNetService)network).ReceiveEvent("3:CANCEL");
                    break;
                }
                await Task.Delay(200);
                if (await MultiplayerService.Instance.internetUtility.IsConnectedToInternet() && !cancellationToken.IsCancellationRequested && waitTime > 0)
                {
                    //((UnityNetService)network).ReceiveEvent("3:Reconnected");
                    OnReconnectedToInternet?.Invoke();
                    disconnected = false;
                    network.SendMove(new GameEvent() { eventType = GameEventType.PLAYER_RECONNECTED });
                    break;
                }
                waitTime -= 0.2f;
            }
            if (waitTime > 0 && !disconnected)
            {
                //Resume game. Ping player for missed moves
                Debug.Log("Bakshish. Reconnected to server. Will wait for other player acknowledgement");
                //((UnityNetService)network).ReceiveEvent("3:Ack Waiting");
                await Task.Delay(2000);
                if (!cancelReconnection)
                {
                    Debug.Log("Bakshish. Reconnected to server. Relay Server Recovery");
                    //((UnityNetService)network).ReceiveEvent("3:Recovery Started");
                    await network.RecoverConnection();
                }
                else
                {
                    cancelReconnection = false;
                }
            }
            else if (waitTime <= 0)
            {
                Debug.Log("No recovery available");
            }
        }
        public bool cancelReconnection;
        public void SetCancellationToken(CancellationToken token)
        {
            cancellationToken = token;
        }

        public void Dispose()
        {
            pingTask.Cancel();
            pingTask.Dispose();
        }
    }
}
