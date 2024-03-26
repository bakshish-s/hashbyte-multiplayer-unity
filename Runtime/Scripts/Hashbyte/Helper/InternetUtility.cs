using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public class InternetUtility
    {

#if UNITY_ANDROID && !UNITY_EDITOR
        public bool IsConnected => checkNetwork.Call<bool>("checkNetwork");
        private AndroidJavaObject checkNetwork;
#else
        public bool IsConnected => true;
#endif
        public InternetUtility()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            checkNetwork = new AndroidJavaObject("com.hashbytestudio.checknetwork.CheckNetwork", currentActivity);
#endif
        }
    }
}
