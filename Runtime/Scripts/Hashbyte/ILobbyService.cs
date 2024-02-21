using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    public interface ILobbyService
    {
        public Task CreateLobby(string lobbyName, int maxPlayers, System.Collections.Hashtable roomProperties);
        public Task<string> JoinLobby(string lobbyId, object additionalData);
        public Task UpdateLobbyData(string lobbyId, System.Collections.Hashtable dataToUpdate);
    }
}