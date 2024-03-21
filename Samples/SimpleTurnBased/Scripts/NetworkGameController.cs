using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Hashbyte.Multiplayer.Demo
{
    public class NetworkGameController : MonoBehaviour, INetworkEvents
    {
        [SerializeField]
        private Transform tilesParent;
        private TileManager tileManager;
        private TurnManager turnManager;
        private void Start()
        {
            tileManager = new TileManager(tilesParent);
            MultiplayerService.Instance.RegisterCallbacks(this);
            turnManager = new TurnManager(null, 0);
            MultiplayerService.Instance.RegisterCallbacks(turnManager);
        }                   

        public void OnPlayerJoined(INetworkPlayer player)
        {
            throw new System.NotImplementedException();
        }       

        public void OnNetworkMessage(GameEvent gameEvent)
        {
            switch (gameEvent.eventType)
            {
                case GameEventType.GAME_STARTED:
                    break;
                case GameEventType.GAME_ENDED:
                    break;
                case GameEventType.GAME_MOVE:

                    break;
                case GameEventType.END_TURN:
                    break;
                case GameEventType.GAME_ALIVE:
                    break;
                default:
                    break;
            }
        }

        void INetworkEvents.OnPlayerConnected()
        {
            
        }

        public void OnRoomPropertiesUpdated(Hashtable roomProperties)
        {
            
        }

        public void OnRoomJoined(GameRoom room )
        {
            throw new System.NotImplementedException();
        }

        public void OnPlayerLeft(INetworkPlayer player)
        {
            throw new System.NotImplementedException();
        }

        public void OnRoomDeleted()
        {
            throw new System.NotImplementedException();
        }
    }
    internal class TileManager
    {
        private Dictionary<int, Tile> gameTiles;
        public TileManager(Transform tilesParent)
        {
            gameTiles = new Dictionary<int, Tile>();
            for (int i = 0; i < tilesParent.childCount; i++)
            {
                Tile tile = tilesParent.GetChild(i).GetComponent<Tile>();
                gameTiles.Add(tile.tileId, tile);
            }
        }

        public Tile GetTile(int tileId) => gameTiles[tileId];
    }
}
