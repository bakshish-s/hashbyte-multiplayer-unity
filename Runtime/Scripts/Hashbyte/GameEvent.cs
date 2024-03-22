namespace Hashbyte.Multiplayer
{
    public struct GameEvent
    {
        public GameEventType eventType;
        public string data;
        public static GameEvent GameStartEvent(string playerId, string extraData = null)
        {
            return new GameEvent()
            {
                eventType = GameEventType.GAME_STARTED,
                data = extraData,
            };
        }
        public static GameEvent GameMoveEvent(string playerId, string extraData)
        {
            return new GameEvent()
            {
                eventType = GameEventType.GAME_MOVE,
                data = extraData
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
        PLAYER_ALIVE,
        PLAYER_ALIVE_RESPONSE,
        PLAYER_RECONNECTED
    }
}