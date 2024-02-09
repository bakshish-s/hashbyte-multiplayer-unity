using UnityEngine.UI;
using UnityEngine;
namespace Hashbyte.Multiplayer.Demo
{
    public class DemoUI : MonoBehaviour
    {
        [SerializeField]
        private Button[] menuButtons;
        [SerializeField]
        private Button[] gameButtons;
        [SerializeField]
        private TMPro.TextMeshProUGUI networkStatus;
        [SerializeField]
        private TMPro.TextMeshProUGUI gameStatus;
        [SerializeField]
        private GameObject lobbyPanel;
        [SerializeField]
        private string gameSceneName;

        private void OnEnable()
        {
            GameboardNetwork.Instance.OnNetworkUpdate += OnNetworkUpdate;
        }

        private void OnNetworkUpdate(NetworkState networkState, string data, GBNetworkError error)
        {
            switch (networkState)
            {
                case NetworkState.CONNECTED:
                    networkStatus.text = $"Connected to Network {data}";
                    gameStatus.text = $"Connected to server, joining game";
                    lobbyPanel.SetActive(true);
                    break;
                case NetworkState.CONNECTING:
                    foreach(Button menuButton in menuButtons) menuButton.interactable = false;
                    networkStatus.text = "Connecting to Network";
                    break;
                case NetworkState.ERROR:
                    if (error != null)
                    {
                        networkStatus.text = $"{error.errorCode}\n{error.errorMessage}";
                    }
                    break;
                case NetworkState.JOINED_LOBBY:
                    networkStatus.text = $"Lobby joined! {data}";
                    gameStatus.text = "Game Joined, Waiting for Players";
                    break;
                case NetworkState.PLAYER_JOINED:
                    networkStatus.text = data;
                    gameStatus.text = "Player Joined, Ready To Start Game";
                    foreach(Button gameButton in gameButtons) gameButton.interactable = GameboardNetwork.Instance.isHost;
                    break;
                case NetworkState.START_GAME:
                    gameStatus.text = "Starting game";
                    Debug.Log("Starting game");
                    UnityEngine.SceneManagement.SceneManager.LoadScene(gameSceneName);
                    break;
                default:
                    break;
            }
        }

        public void StartMultiplayer()
        {
            GameboardNetwork.Instance.JoinRandomGame();
        }

        public void StartGame()
        {
            GameboardNetwork.Instance.SendGameStartedEvent();
        }
    }
}
