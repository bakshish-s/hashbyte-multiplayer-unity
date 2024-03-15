using Hashbyte.Multiplayer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Hashbyte.Multiplayer
{
    public interface ITurnEvents
    {
        public void OnNetworkMessage(GameEvent gameEvent);
    }
}
