using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
namespace Hashbyte.Multiplayer.Demo
{
    public class DemoGame : MonoBehaviour
    {
        [SerializeField]
        List<Button> gridItems;

        #region Initialization
        private void OnEnable()
        {
            GameboardNetwork.Instance.OnGameEvent += OnGameEvent;
        }

        private void OnDisable()
        {
            GameboardNetwork.Instance.OnGameEvent -= OnGameEvent;
        }
        #endregion

        public void SendGameEvent(Button button)
        {
            button.GetComponent<Image>().color = Color.blue;
            GameboardNetwork.Instance.SendGameMove(gridItems.IndexOf(button).ToString());
        }

        private void OnGameEvent(GameEvent gameEvent)
        {
            switch (gameEvent.eventType)
            {
                case GameEventType.GAME_MOVE:
                    ProcessGameMove(gameEvent);
                    break;
                default:
                    break;
            }

        }

        private void ProcessGameMove(GameEvent move)
        {
            int index = -1;
            int.TryParse(move.data, out index);
            if (index != -1)
            {
                gridItems[index].GetComponent<Image>().color = Color.yellow;
            }
        }
    }
}