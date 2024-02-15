using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public class HashbyteNetworkHelper : MonoBehaviour
    {
        public bool initializeOnStart;
        public ServiceType serviceType = ServiceType.UNITY;

        private void Awake()
        {
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
