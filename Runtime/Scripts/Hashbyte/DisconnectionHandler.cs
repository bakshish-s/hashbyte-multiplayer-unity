using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    internal class DisconnectionHandler
    {
        private float timeForNextPing = 5;
        private bool pingReceived, pongReceived;
        private GameEvent ping = new GameEvent() { eventType = GameEventType.PING };
        private GameEvent pong = new GameEvent() { eventType = GameEventType.PONG };
        private INetworkService network;
        private CancellationToken cancellationToken;
        private const float eventTime = 2;

        public delegate void DisconnectionEvents();
        public event DisconnectionEvents OnDisconnectedFromInternet, NoResponseFromOpponent, OpponentReconnected, OnReconnectedToInternet;

        public DisconnectionHandler(INetworkService _network)
        {
            network = _network;
        }

        public async void SendPing()
        {
            //Wait one second before sending next ping
            await Task.Delay(1000);
            pongReceived = false;
            timeForNextPing = eventTime;
            DateTime startTime = DateTime.Now;
            network.SendMove(ping);
            GameEvent gameEvent = new GameEvent() { eventType = GameEventType.GAME_ALIVE };
            while (!pongReceived && timeForNextPing > 0)
            {
                await Task.Yield();
                timeForNextPing = (float)(eventTime - (DateTime.Now - startTime).TotalSeconds);
            }
            //Either we are not connected or other player not connected to internet
            if (!pongReceived)
            {
                Debug.Log($"Client not responded in 2 seconds, Checking my internet connection");
                ((UnityNetService)network).ReceiveEvent("3:Resp Missing");
                if (!await MultiplayerService.Instance.internetUtility.IsConnectedToInternet())
                {
                    Debug.Log($"I am not connected to internet");
                    ((UnityNetService)network).ReceiveEvent("3:No Internet");
                    OnDisconnectedFromInternet?.Invoke();
                    TryReconnecting();
                }
                else
                {
                    Debug.Log($"I am connected to internet, other player not connected");
                    ((UnityNetService)network).ReceiveEvent("3:No Response");
                    NoResponseFromOpponent?.Invoke();
                }
            }

        }

        public async void CheckPing()
        {
            await Task.Delay(900);
            pingReceived = false;
            timeForNextPing = eventTime;
            DateTime startTime = DateTime.Now;
            while (!pingReceived && timeForNextPing > 0)
            {
                await Task.Yield();
                timeForNextPing = (float)(eventTime - (DateTime.Now - startTime).TotalSeconds);
            }
            if (!pingReceived)
            {
                Debug.Log($"Host not responded in 2 seconds, Checking my internet connection");
                ((UnityNetService)network).ReceiveEvent("3:Resp Missing");
                if (!await MultiplayerService.Instance.internetUtility.IsConnectedToInternet())
                {
                    Debug.Log($"I am not connected to internet");
                    ((UnityNetService)network).ReceiveEvent("3:No Internet");
                    OnDisconnectedFromInternet?.Invoke();
                    TryReconnecting();
                }
                else
                {
                    Debug.Log($"I am connected to internet, other player not connected");
                    ((UnityNetService)network).ReceiveEvent("3:No Response");
                    NoResponseFromOpponent?.Invoke();
                }
            }
        }

        public void OnPing()
        {
            //Host sent us a ping, to confirm we are alive, send pong back                        
            pingReceived = true;
            network.SendMove(pong);
            CheckPing();
        }
        public void OnPong()
        {
            //Other player confirmed connected by sending pong back
            pongReceived = true;
            //Resend ping to player to keep connection alive
            SendPing();
        }

        public void OnReconnected(bool isHost)
        {
            OpponentReconnected?.Invoke();
            //Other player reconnected, we establish ping again
            if (isHost) SendPing();
            else
            {
                pingReceived = true;
                network.SendMove(pong);
            }
        }

        private async void TryReconnecting()
        {
            float waitTime = 60/*seconds*/;
            Debug.Log("Bakshish. Trying to reconnect");
            ((UnityNetService)network).ReceiveEvent("3:Reconnecting");
            bool disconnected = true;
            while (waitTime > 0)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    ((UnityNetService)network).ReceiveEvent("3:CANCEL");
                    break;
                }
                await Task.Delay(200);
                if (await MultiplayerService.Instance.internetUtility.IsConnectedToInternet() && !cancellationToken.IsCancellationRequested && waitTime > 0)
                {
                    ((UnityNetService)network).ReceiveEvent("3:Reconnected");
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
                ((UnityNetService)network).ReceiveEvent("3:Ack Waiting");
                await Task.Delay(2000);
                if (!cancelReconnection)
                {                    
                    Debug.Log("Bakshish. Reconnected to server. Relay Server Recovery");
                    ((UnityNetService)network).ReceiveEvent("3:Recovery Started");
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
    }
}
