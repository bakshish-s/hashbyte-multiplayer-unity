using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
namespace Hashbyte.Multiplayer
{
    public class UnityAuthService : IAuthService
    {
        public bool IsInitialized { get; private set; }

        public string PlayerId => AuthenticationService.Instance.PlayerId;

        public async Task Authenticate()
        {            
            //Initialize unity services
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"User authenticated with ID {AuthenticationService.Instance.PlayerId}");
            IsInitialized = true;
        }

        public async Task AuthenticateWith(INetworkPlayer networkPlayer)
        {            
            if (string.IsNullOrEmpty(networkPlayer.PlayerId))
                await Authenticate();
            else
            {
                InitializationOptions options = new InitializationOptions();
                options.SetProfile(networkPlayer.PlayerId);
                await UnityServices.InitializeAsync(options);
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"User {options} authenticated with ID {AuthenticationService.Instance.PlayerId}");
                IsInitialized = true;
            }                
        }
    }
}
