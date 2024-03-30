using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public class InternetUtility
    {
        public bool IsConnected { get; private set; }
        private CancellationTokenSource source;
        public delegate void ConnectionStatusChange(bool connected);
        public event ConnectionStatusChange OnConnectionStatusChange;
#if UNITY_EDITOR
        private bool IsInternallyConnected => true;
# elif UNITY_ANDROID
        public bool IsInternallyConnected
        {
            get
            {
                return checkNetwork.Call<bool>("checkNetwork");
            }
        }
        private AndroidJavaObject checkNetwork;
#endif
        private int disconnectCount = 0;
        public InternetUtility()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            checkNetwork = new AndroidJavaObject("com.hashbytestudio.checknetwork.CheckNetwork", currentActivity);
#endif
            IsConnected = IsInternallyConnected;
            OnConnectionStatusChange?.Invoke(IsConnected);
            source = new CancellationTokenSource();
            StartNetworkCheck(source.Token);
        }
        public async void StartNetworkCheck(CancellationToken token)
        {
            while (!token.IsCancellationRequested && IsInternallyConnected)
            {
                await CheckPing(token);
                await Task.Delay(3000);
                //Debug.Log($"After checking ping {IsConnected}");
            }
            if (IsConnected)
            {
                IsConnected = false;
                OnConnectionStatusChange?.Invoke(IsConnected);
            }
            WaitForReconnection(token);
            Debug.Log($"Out of while loop {IsInternallyConnected}");
        }
        private async void WaitForReconnection(CancellationToken token)
        {
            float timer = 30;
            while (!token.IsCancellationRequested && !IsInternallyConnected && timer > 0)
            {
                await Task.Yield();
                timer -= Time.deltaTime;
            }
            if (timer > 0 && !token.IsCancellationRequested)
            {
                StartNetworkCheck(token);
            }
            else
            {
                Debug.Log($"Failed to reconnect in 30 seconds");
                //TODO Bakshish:Should do something here if not connected back in 30 seconds
            }
        }
        public void Dispose()
        {
            Debug.Log("****************************** PING: CANCEL *********************************");
            source.Cancel();
        }
        float timeout = 2;
        public async Task CheckPing(CancellationToken token)
        {
            try
            {
                Ping ping = new Ping("8.8.8.8");
                //Debug.Log($"PING: START {timeout}");
                while (!token.IsCancellationRequested && !ping.isDone && timeout > 0)
                {
                    await Task.Yield();
                    timeout -= Time.deltaTime;
                }
                //Debug.Log($"PING: END {timeout} -- {ping.time}");
                timeout = 2;
                if (ping.isDone && ping.time != -1)
                {
                    //Debug.Log($"Ping success {IsConnected}");
                    if (!IsConnected)
                    {
                        IsConnected = true;
                        OnConnectionStatusChange?.Invoke(true);
                    }
                }
                else
                {
                    //Debug.Log($"Ping failed {IsConnected}");
                    if (IsConnected)
                    {
                        disconnectCount++;
                        if (disconnectCount > 1)
                        {
                            IsConnected = false;
                            OnConnectionStatusChange?.Invoke(false);
                            disconnectCount = 0;
                        }
                    }
                }
                ping.DestroyPing();
            }
            catch (System.Exception e)
            {
                Debug.Log("Error while pingin " + e.Message);
            }
        }
    }
}
