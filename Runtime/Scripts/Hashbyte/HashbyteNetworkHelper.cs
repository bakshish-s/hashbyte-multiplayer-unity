using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public class HashbyteNetworkHelper : MonoBehaviour
    {
        public bool initializeOnStart;
        public ServiceType serviceType = ServiceType.UNITY;
        public static HashbyteNetworkHelper Instance { get; private set; }

        private void Awake()
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
            if (initializeOnStart) HashbyteNetwork.Instance.Initialize(serviceType);
        }        

        // Update is called once per frame
        void Update()
        {
            HashbyteNetwork.Instance.Update();
        }

        private void OnDestroy()
        {
            HashbyteNetwork.Instance.Dispose();
        }
    }
}
