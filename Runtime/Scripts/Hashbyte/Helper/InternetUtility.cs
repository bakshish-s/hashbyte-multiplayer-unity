using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public class InternetUtility
    {
        public bool IsConnected => checkNetwork.Call<bool>("checkNetwork");
#if UNITY_ANDROID
        private AndroidJavaObject checkNetwork;
#endif
        public InternetUtility()
        {
#if UNITY_ANDROID
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            checkNetwork = new AndroidJavaObject("com.hashbytestudio.checknetwork.CheckNetwork", currentActivity);
#endif
        }
    }
}
