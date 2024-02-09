using System.Threading.Tasks;

namespace Hashbyte.Multiplayer
{
    public abstract class MultiplayerService
    {        
        public abstract void JoinRandomGame();
        public virtual async Task InitAndAuthenticate() { }
    }
} 
