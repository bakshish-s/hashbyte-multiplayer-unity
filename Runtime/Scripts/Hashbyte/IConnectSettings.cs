namespace Hashbyte.Multiplayer
{
    public interface IConnectSettings
    {
        public string ConnectionType { get; set; }
        public IRoomResponse RoomResponse { get; set; }
        public void Initialize(string connectionType, IRoomResponse roomResponse);
    }
}
