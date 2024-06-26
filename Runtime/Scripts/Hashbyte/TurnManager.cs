using System.Collections;
using System.Collections.Generic;

namespace Hashbyte.Multiplayer
{
    public class TurnManager: INetworkEvents
    {
        private List<INetworkPlayer> _players;
        private int currentTurn;
        public TurnManager(List<INetworkPlayer> networkPlayers, int firstTurn)
        {
            _players = networkPlayers;
            currentTurn = firstTurn;
        }

        public void OnNetworkMessage(GameEvent gameEvent)
        {
            if(gameEvent.eventType == GameEventType.END_TURN)
            {
                CalculateTurn();
            }
            
        }

        private void CalculateTurn()
        {
            currentTurn++;
            if(currentTurn >= _players.Count)
            {
                currentTurn = 0;
                for(int i=0; i< _players.Count; i++)
                {
                    _players[i].OnTurnUpdate(i==currentTurn);
                }
            }
        }

        public void OnPlayerConnected()
        {
            throw new System.NotImplementedException();
        }

        public void OnPlayerJoined(INetworkPlayer playerName)
        {
            throw new System.NotImplementedException();
        }

        public void OnRoomPropertiesUpdated(Hashtable roomProperties)
        {
            throw new System.NotImplementedException();
        }

        public void OnRoomJoined(GameRoom room)
        {
            
        }

        public void OnPlayerLeft(INetworkPlayer player)
        {
            throw new System.NotImplementedException();
        }

        public void OnRoomDeleted()
        {
            throw new System.NotImplementedException();
        }

        public void OnPlayerDisconnected()
        {
            throw new System.NotImplementedException();
        }

        public void OnPlayerReconnected()
        {
            throw new System.NotImplementedException();
        }

        public void OnConnectionStatusChange(bool connected)
        {
            throw new System.NotImplementedException();
        }

        public void OnRoomJoinFailed(FailureReason reason)
        {
            throw new System.NotImplementedException();
        }
    }
}
