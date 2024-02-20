using UnityEngine;
namespace Hashbyte.Multiplayer.Demo
{
    public class Tile : MonoBehaviour
    {
        public int tileId { get; private set; }

        private UnityEngine.UI.Image tileImage;
        private void Awake()
        {
            tileId = transform.GetSiblingIndex();
            GetComponent<UnityEngine.UI.Button>().onClick.AddListener(OnClicked);
            tileImage = transform.GetChild(0).GetComponent<UnityEngine.UI.Image>();
        }

        public void OnClicked()
        {
            if (!Player.Instance.isMyTurn) return;
            ClickTile();
            Player.Instance.SendMove(tileId);
        }
        public void ClickTile()
        {
            tileImage.color = Player.Instance.myTileColor;
        }

    }
}
