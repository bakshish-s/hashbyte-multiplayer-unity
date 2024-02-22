using System.Threading.Tasks;
namespace Hashbyte.Multiplayer
{
    public interface IAuthService
    {
        public bool IsInitialized { get; }
        public string PlayerId { get; }
        public Task Authenticate();
        //To be implemented later
        public Task AuthenticateWith(INetworkPlayer networkPlayer);
    }
}
