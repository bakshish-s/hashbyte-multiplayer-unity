using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    //Responsibility: To provide access to different components of multiplayer services like Authentication, Lobby, Relay, Matchmaking etc.
    public abstract class MultiplayerService
    {
        protected bool isInitialized => authService.IsInitialized;
        protected abstract IAuthService authService { get; set; }
        protected abstract IGameRoomService roomService { get; set; }
        protected abstract INetworkService networkService { get; set; }
        public abstract Task Initialize();
        /// <summary>
        /// Joins any game available
        /// </summary>
        /// <returns>Returns room code if a game is joined or empty string if not available</returns>
        public abstract Task<string> JoinRandomGame();
        //Start calling update as soon as Multiplayer service is initialized
        public abstract void Update();
        public void CreatePrivateGame() { }
        public void JoinPrivateGame() { }
        public void OnPlayerJoined() { }
        public void OnPlayerLeft() { }
        public void OnPlayerDisconnected() { }
    }
} 
