using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public class UnityClientNetService : INetworkService
    {      
        public void JoinSession(string sessionId)
        {
            throw new System.NotImplementedException();
        }

        public void NetworkUpdate()
        {
            throw new System.NotImplementedException();
        }

        bool INetworkService.ConnectToServer()
        {
            throw new System.NotImplementedException();
        }

        Task<string> INetworkService.CreateSession(string region)
        {
            throw new System.NotImplementedException();
        }
    }
}
