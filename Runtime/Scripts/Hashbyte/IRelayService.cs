using System.Threading.Tasks;
namespace Hashbyte.Multiplayer
{
    public interface IRelayService
    {
        /// <summary>
        /// Creates a new session on Relay server
        /// </summary>
        /// <param name="region">Requires region in which relay session should be created</param>
        /// <returns>returns session ID of relay session created</returns>
        public Task<string> CreateRelaySession(string region);
        public Task JoinRelaySession(string sessionId);
    }
}
