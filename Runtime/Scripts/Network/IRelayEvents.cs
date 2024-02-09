namespace Hashbyte.Multiplayer
{
    public interface IRelayEvents
    {
        void OnGameSessionCreated(string sessionId);
        void OnGameSessionJoined();
        void OnConnectedToServer();
        void OnConnectedToHost();
        void OnNetworkError(NetworkErrorCode code, string message);
        void OnEvent(GameEvent gameEvent);
    }
}
