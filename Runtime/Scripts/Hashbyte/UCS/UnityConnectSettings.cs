namespace Hashbyte.Multiplayer
{
    public class UnityConnectSettings : IConnectSettings
    {
        public string ConnectionType { get; set; }
        public IRoomResponse RoomResponse { get; set; }

        public void Initialize(string connectionType, IRoomResponse roomResponse)
        {
            this.ConnectionType = connectionType;
            this.RoomResponse = roomResponse;   
        }
    }
}
