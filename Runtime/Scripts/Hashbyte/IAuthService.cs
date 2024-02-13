using System.Threading.Tasks;

public interface IAuthService
{
    public bool IsInitialized { get; }
    public Task Authenticate();
    //To be implemented later
    public Task AuthenticateWith();
}
