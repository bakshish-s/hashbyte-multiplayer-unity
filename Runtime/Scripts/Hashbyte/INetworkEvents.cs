
namespace Hashbyte.Multiplayer
{
    public interface INetworkEvents
    {
        public void OnPlayerJoined(string playerName);
        public void OnPlayerConnected();
        public void OnRoomJoined(GameRoom roomJoined);
        public void OnRoomPropertiesUpdated(System.Collections.Hashtable roomProperties) ;
        public void OnNetworkMessage(GameEvent gameEvent) ;
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