namespace Hashbyte.Multiplayer
{
    public struct GameEvent
    {
        public GameEventType eventType;
        public DataSize size;
        public string data;
        public static GameEvent GameStartEvent(string playerId, string extraData = null, DataSize dataSize = DataSize.NORMAL)
        {
            return new GameEvent()
            {
                eventType = GameEventType.GAME_STARTED,
                data = extraData,
                size = dataSize,
            };
        }
        public static GameEvent GameMoveEvent(string playerId, string extraData)
        {
            return new GameEvent()
            {
                eventType = GameEventType.GAME_MOVE,
                data = extraData,                
            };
        }
    }

    public enum GameEventType
    {
        GAME_STARTED = 1,
        GAME_ENDED,
        GAME_MOVE,
        END_TURN,
        GAME_ALIVE,
        PING,
        PONG,
        PLAYER_RECONNECTED,
        RECONNECTION_ACKNOWLEDGE,
        CONNECT_FAILED,
    }

    public enum DataSize
    {
        NORMAL,
        MEDIUM,
        LARGE
    }
}