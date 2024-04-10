namespace Hashbyte.Multiplayer
{
    internal interface IMultiplayerEvents
    {
        public void JoinRoomResponse(IRoomResponse roomResponse);           
        public void OnPlayerJoinedRoom(System.Collections.Generic.List<INetworkPlayer> playersJoined);
        public void OnPlayerLeftRoom(System.Collections.Generic.List<int> playerIndices);
        public void OnPlayerConnected();        
        public void OnReconnected();
        public void LostConnection();
        public void OtherPlayerNotResponding();
        public void OnOtherPlayerReconnected();
        public void OnRoomDeleted();
        public void OnRoomDataUpdated(System.Collections.Hashtable data);
        public void OnRoomJoinFailed(FailureReason failureReason);
        public System.Collections.Generic.List<INetworkEvents> GetTurnEventListeners();
    }

    public enum FailureReason
    {
        INVALID_CODE = 16010,
        LOBBY_NOT_FOUND = 16001,
        ROOM_FULL = 0,
    }
}
