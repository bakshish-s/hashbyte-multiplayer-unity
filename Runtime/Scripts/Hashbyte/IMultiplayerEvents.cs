namespace Hashbyte.Multiplayer
{
    internal interface IMultiplayerEvents
    {
        public void JoinRoomResponse(IRoomResponse roomResponse);   
        public void CreateRoomResponse(IRoomResponse roomResponse);
        public void OnPlayerJoinedRoom(System.Collections.Generic.List<string> playersJoined);
    }
}
