using Hashbyte.Multiplayer.Demo;
using System.Collections.Generic;
using UnityEngine;
namespace Hashbyte.Multiplayer.Demo
{
    public class Player : MonoBehaviour, INetworkPlayer
    {
        #region Singelton
        public static Player Instance { get; private set; }
        private void Awake()
        {            
            Instance = this;
        }
        #endregion

        public bool isMyTurn { get; private set; }
        public Color myTileColor { get; private set; }
        private void Start()
        {                        
            myTileColor = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));  
        }

        public string PlayerId => GetHashCode().ToString();

        public int ActorNumber {get; set;}

        public string PlayerName => throw new System.NotImplementedException();

        public void JoinedGame(string roomId)
        {
            
        }

        public void OnTurnUpdate(bool isMyTurn)
        {
            this.isMyTurn = isMyTurn;
        }        

        public void SendMove(int tileId)
        {
            MultiplayerService.Instance.SendMove(new GameEvent()
            {
                data = tileId.ToString(),
                eventType = GameEventType.GAME_MOVE,
            });            
        }
    }
}

