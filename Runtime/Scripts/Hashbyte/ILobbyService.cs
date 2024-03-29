using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    public interface ILobbyService
    {
        public Task<string> CreateLobby(string lobbyName, int maxPlayers, System.Collections.Hashtable roomProperties, bool isPrivate);
        public Task<string> JoinLobby(string lobbyId, System.Collections.Hashtable options);
        public Task UpdateLobbyData(string lobbyId, System.Collections.Hashtable dataToUpdate);
    }
}