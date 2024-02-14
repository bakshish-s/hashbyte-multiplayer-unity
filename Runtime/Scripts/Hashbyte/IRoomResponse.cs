namespace Hashbyte.Multiplayer
{
    public interface IRoomResponse
    {
        public string RoomId { get; }
        public bool Success { get; }
        public RoomError Error { get; }
        public bool isHost { get; }

    }
    public struct RoomError
    {
        public int ErrorCode;
        public string Message;
    }
}