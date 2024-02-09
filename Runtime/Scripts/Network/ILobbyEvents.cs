using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
namespace Hashbyte.Multiplayer
{
    public interface ILobbyEvents
    {
        void OnLobbyException(LobbyExceptionReason reason, ErrorStatus error);
        void OnPlayersJoined(System.Collections.Generic.List<LobbyPlayerJoined> playersJoined);
    }
}