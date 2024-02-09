using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;

namespace Hashbyte.Multiplayer
{
    public class UnityMultiplayerService : MultiplayerService
    {
        public override void JoinRandomGame()
        {
            
        }

        public override async Task InitAndAuthenticate()
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
}
