using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public class HashbyteNetworkHelper : MonoBehaviour
    {
        public bool initializeOnStart;
        public ServiceType serviceType = ServiceType.UNITY;
        public static HashbyteNetworkHelper Instance { get; private set; }

        private async void Awake()
        {
            if(Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            if (initializeOnStart) await MultiplayerService.Instance.Initialize(null, null);//HashbyteNetwork.Instance.Initialize(serviceType);
        }        

        // Update is called once per frame
        void Update()
        {
            MultiplayerService.Instance.Update();
        }

        private void OnDestroy()
        {
            MultiplayerService.Instance.Dispose();
        }
    }
}
