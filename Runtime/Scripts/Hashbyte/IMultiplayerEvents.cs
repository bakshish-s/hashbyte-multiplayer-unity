namespace Hashbyte.Multiplayer
{
    internal interface IMultiplayerEvents
    {
        public void JoinRoomResponse(IRoomResponse roomResponse);           
        public void OnPlayerJoinedRoom(System.Collections.Generic.List<INetworkPlayer> playersJoined);
        public void OnPlayerLeftRoom(System.Collections.Generic.List<int> playerIndices);
        public void OnPlayerConnected();
        public void OnRoomDeleted();
        public System.Collections.Generic.List<INetworkEvents> GetTurnEventListeners();
    }
}
