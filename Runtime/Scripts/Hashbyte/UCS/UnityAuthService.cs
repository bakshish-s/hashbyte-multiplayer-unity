using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class UnityAuthService : IAuthService
{
    public bool IsInitialized {get; private set;}
    public async Task Authenticate()
    {
        //Initialize unity services
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
        IsInitialized = true;
    }

    public Task AuthenticateWith()
    {
        throw new System.NotImplementedException();
    }
}
