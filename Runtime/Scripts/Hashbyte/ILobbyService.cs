using System.Collections;
using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    public interface ILobbyService
    {
        public Task CreateLobby(string lobbyName, int maxPlayers, object additionalData);
        public Task<string> JoinLobby(string lobbyId, object additionalData);
        public Task UpdateLobbyData(string lobbyId, Hashtable dataToUpdate);
    }
}