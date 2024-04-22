using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public class InternetUtility
    {
        private CancellationTokenSource source;
        public delegate void InternetStatus(string status);
        public event InternetStatus OnStatus;
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
            source = new CancellationTokenSource();
        }
        public void Dispose()
        {
            source.Cancel();
        }

        public async Task<bool> IsConnectedToInternet(CancellationToken token = default)
        {
            if (token == default) token = source.Token;
            //#1 Check Unity's in build internt check, if it returns false, we definitely not connected to internet
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                //Debug.Log($"ThorHammer: Checking internet (UNITY)");
                OnStatus?.Invoke($"NO INTERNET UNITY");
                return false;
            }
            //#2 If Android plugin returns false, that ensures internet is not connected
            else if (!IsInternallyConnected)
            {
                //Debug.Log($"ThorHammer: Checking internet (PLUGIN)");
                OnStatus?.Invoke($"NO INTERNET PLUGIN");
                return false;
            }
            else
            {
                //OnStatus?.Invoke("Unity and Plugin Says Connected");
                //Both Unity and Android plugin confirmed we are connected to internet
                //We ensure internet is reachable with final step of pinging google dns
                int tryCount = 0;
                while (tryCount < 2 && !token.IsCancellationRequested)
                {
                    tryCount++;
                    OnStatus?.Invoke($"Ping {tryCount}");
                    Ping ping = new Ping("8.8.8.8");
                    float timeout = 0;//3 seconds
                    System.DateTime currentTime = System.DateTime.Now;
                    while (!ping.isDone && timeout < 2000 && !token.IsCancellationRequested)
                    {
                        timeout = (System.DateTime.Now - currentTime).Milliseconds;
                        await Task.Yield();
                    }
                    if (ping.isDone && ping.time != -1)
                    {
                        OnStatus?.Invoke($"INTERNET AVAILABLE");
                        //Debug.Log($"ThorHammer: Ping {tryCount} returned success");
                        //We are connected to internet
                        return true;
                    }
                    OnStatus?.Invoke($"Ping Completed {tryCount}");
                }
                //Debug.Log($"ThorHammer: Ping failed {token.IsCancellationRequested}");
                OnStatus?.Invoke($"NO INTERNET PING {token.IsCancellationRequested}");
            }
            return false;
        }
    }
}
