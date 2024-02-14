namespace Hashbyte.Multiplayer
{
    public class UnityConnectSettings : IConnectSettings
    {
        public string ConnectionType { get; set; }
        public UnityRoomResponse roomResponse;
    }
}
