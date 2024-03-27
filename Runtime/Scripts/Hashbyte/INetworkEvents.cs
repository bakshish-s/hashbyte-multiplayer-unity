
namespace Hashbyte.Multiplayer
{
    public interface INetworkEvents : ITurnEvents
    {
        public void OnPlayerJoined(INetworkPlayer player);
        public void OnPlayerLeft(INetworkPlayer player);
        public void OnPlayerConnected();
        public void OnPlayerDisconnected();
        public void OnPlayerReconnected();
        public void OnConnectionStatusChange(bool connected);
        public void OnRoomJoined(GameRoom roomJoined);
        public void OnRoomDeleted();
        public void OnRoomPropertiesUpdated(System.Collections.Hashtable roomProperties) ;                
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