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
        public bool stopPing;
        #region Ping paramteres 
        private DateTime startTime;
        private int pingId;
        private int pingReceivedId;
        private int pongReceivedId;

        #endregion

        public delegate void DisconnectionEvents();
        public event DisconnectionEvents OnDisconnectedFromInternet, NoResponseFromOpponent, OpponentReconnected, OnReconnectedToInternet;
        public bool opponentNotResponding;
        public DisconnectionHandler(INetworkService _network)
        {
            network = _network;
        }
        #region Host Ping System
        public async void HeartbeatClient()
        {
            if (stopPing) return;
            //Wait half a second for things to reset before sending next heartbeat
            //int randomDelay = UnityEngine.Random.Range(800, 7000);
            await Task.Delay(1000);
            //Debug.Log($"Heartbeating");
            pingId++;
            pongReceived = false;
            string timeE = pongReceivedId.ToString();
            int pingCount = 1;
            while (pingCount <= 3 && !pongReceived && cancellationToken != null && !cancellationToken.IsCancellationRequested)
            {
                //Ping client and wait for response
                ping.data = pingId.ToString();
                //Debug.Log($"Ping sent to client {ping.data} {cancellationToken.IsCancellationRequested}");
                bool clientResponded = await PingClient();
                if (stopPing) return;
                //Debug.Log($"Ping response {clientResponded}");
                if (clientResponded)
                {
                    break;
                }
                else
                {
                    pingCount++;
                    //Debug.Log($"ThorHammer 2 seconds, checking internet {pongReceivedId}/{pingCount} - {timeE}");
                    if (!await CheckInternet())
                    {
                        //Debug.Log($"ThorHammer Our internet connection is not working");
                        break;
                    }
                    if (stopPing) return;
                    //Our internet is connected, try reaching client again
                    //Debug.Log($"ThorHammer Our internet is connected, sending ping again {pingCount} ");
                }
            }
            //Debug.Log($"Out of while loop {timeE}");
            //Client really not connected to internet
            if (pingCount > 3)
            {
                opponentNotResponding = true;
                NoResponseFromOpponent?.Invoke();
            }
        }

        private async Task<bool> PingClient()
        {
            startTime = DateTime.Now;
            waitTime = timeBetweenPings;
            network.SendMove(ping);
            while (!pongReceived && waitTime > 0 && cancellationToken != null && !cancellationToken.IsCancellationRequested)
            {
                await Task.Yield();
                waitTime = (float)(timeBetweenPings - (DateTime.Now - startTime).TotalSeconds);
            }
            //Check if client responded
            if (cancellationToken != null && !cancellationToken.IsCancellationRequested)
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
            if (stopPing) return;
            await Task.Delay(1000);
            pingReceived = false;
            int waitCount = 1;
            if (stopPing) return;
            while (waitCount <= 3 && cancellationToken != null && !cancellationToken.IsCancellationRequested)
            {
                startTime = DateTime.Now;
                waitTime = maxTimeWithoutPing;
                while (!pingReceived && waitTime > 0 && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Yield();
                    waitTime = (float)(maxTimeWithoutPing - (DateTime.Now - startTime).TotalSeconds);
                }
                if (stopPing) return;
                if (pingReceived)
                {
                    break;
                }
                else
                {
                    //Check internet connection
                    //Debug.Log($"ThorHammer Ping not received in 2 seconds {waitCount}");
                    waitCount++;
                    if (!await CheckInternet())
                    {
                        //Debug.Log($"ThorHammer: Our internet is not connected");
                        break;
                    }
                    if (stopPing) return;
                    //Debug.Log($"ThorHammer Our internet is connected, check for ping again {pingReceivedId}--{cancellationToken.IsCancellationRequested}");
                }
            }
            if (waitCount > 3)
            {
                opponentNotResponding = true;
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
            try
            {
                if (!await MultiplayerService.Instance.internetUtility.IsConnectedToInternet(cancellationToken))
                {
                    //Either internet is not connected or pong received
                    if (cancellationToken.IsCancellationRequested)
                    {
                        //We don't need to recover anything, we need to get out from here                        
                        return false;
                    }
                    else
                    {
                        //Internet is not connected
                        OnDisconnectedFromInternet?.Invoke();
                        TryReconnecting();
                        return false;
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (ObjectDisposedException e)
            {
                //Debug.Log($"Already disposed " + e);
                return false;
            }
        }
        #endregion
        public void OnReconnected(bool isHost)
        {
            OpponentReconnected?.Invoke();
            opponentNotResponding = false;
            //Other player reconnected, we establish ping again
            if (isHost) HeartbeatClient();
            else
            {
                pingReceived = true;
                pong.data = pingReceivedId.ToString();
                network.SendMove(pong);
            }
        }
        public void StillAlive(bool isHost)
        {
            if (opponentNotResponding)
            {
                if (isHost)
                {
                    //if (pongReceivedId == 0)
                    {
                        //Has not received pong
                        OnReconnected(isHost);
                    }
                }
                else
                {
                    //if (pingReceivedId == 0)
                    {
                        //Has not received ping
                        OnReconnected(isHost);
                    }
                }
            }
        }
        private async void TryReconnecting()
        {
            float waitTime = 60/*seconds*/;
            //Debug.Log("ThorHammer: Trying to reconnect");
            //((UnityNetService)network).ReceiveEvent("3:Reconnecting");
            bool disconnected = true;
            while (waitTime > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    //((UnityNetService)network).ReceiveEvent("3:CANCEL");
                    break;
                }
                await Task.Delay(100);
                if (await MultiplayerService.Instance.internetUtility.IsConnectedToInternet() && !cancellationToken.IsCancellationRequested && waitTime > 0)
                {
                    //((UnityNetService)network).ReceiveEvent("3:Reconnected");
                    OnReconnectedToInternet?.Invoke();
                    disconnected = false;
                    network.SendMove(new GameEvent() { eventType = GameEventType.PLAYER_RECONNECTED });
                    break;
                }
                waitTime -= 0.1f;
            }
            if (waitTime > 0 && !disconnected)
            {
                //Resume game. Ping player for missed moves
                //Debug.Log("ThorHammer. Reconnected to server. Will wait for other player acknowledgement");
                //((UnityNetService)network).ReceiveEvent("3:Ack Waiting");
                await Task.Delay(2000);
                if (!cancelReconnection)
                {
                    //Debug.Log("ThorHammer. Reconnected to server. Relay Server Recovery");
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
                //Debug.Log("ThorHammer: No recovery available");
            }
        }
        public bool cancelReconnection;
        public void SetCancellationToken(CancellationToken token)
        {
            cancellationToken = token;
        }
    }
}
