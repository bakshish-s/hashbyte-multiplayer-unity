namespace Hashbyte.Multiplayer
{
    public interface INetworkEvents
    {
        void OnSignIn(string playerId);
        void OnGameSessionCreated(string sessionId);
        void OnConnectedToServer();
        void OnNetworkError(NetworkErrorCode code, string message);
    }

    public enum NetworkErrorCode
    {
        BINDING_FAILED,
        LISTENING_FAILED
    }

    public enum NetworkState
    {
        UNKNOWN,
        NOT_CONNECTED,
        CONNECTING,
        CONNECTED,
        DISCONNECTED,
        JOINING_LOBBY,
        JOINED_LOBBY,
        PLAYER_JOINED,
        START_GAME,
        ERROR
    }

    public class GBNetworkError
    {
        public int errorCode;
        public string errorMessage;
    }
}