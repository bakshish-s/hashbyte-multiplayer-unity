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
        private const float eventTime = 5;

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
                if (!MultiplayerService.Instance.IsConnected)
                {
                    //I lost connection to internet. Immediately tell user
                    OnDisconnectedFromInternet?.Invoke();
                    TryReconnecting();
                    break;
                }
                timeForNextPing = (float)(20 - (DateTime.Now - startTime).TotalSeconds);
            }
            if (!pongReceived && MultiplayerService.Instance.IsConnected)
            {
                NoResponseFromOpponent?.Invoke();
            }
        }

        public async void CheckPing()
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
                    OnDisconnectedFromInternet?.Invoke();
                    TryReconnecting();
                    break;
                }
                timeForNextPing = (float)(5 - (DateTime.Now - startTime).TotalSeconds);
            }
            if (!pingReceived && MultiplayerService.Instance.IsConnected)
            {
                NoResponseFromOpponent?.Invoke();
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
            bool disconnected = true;
            while (waitTime > 0)
            {
                await Task.Delay(200);
                if (MultiplayerService.Instance.IsConnected && !cancellationToken.IsCancellationRequested && waitTime > 0)
                {
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
                Debug.Log("Bakshish. Reconnected to server. Handle Relay issue here");
                network.RecoverConnection();
            }else if(waitTime <= 0)
            {
                Debug.Log("No recovery available");
            }
        }

        public void SetCancellationToken(CancellationToken token)
        {
            cancellationToken = token;
        }
    }
}
