using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public class UnityClientNetService : INetworkService
    {    
        public void NetworkUpdate()
        {
            throw new System.NotImplementedException();
        }

        public bool ConnectToServer(IConnectSettings connectSettings)
        {
            return false;
        }
    }
}
