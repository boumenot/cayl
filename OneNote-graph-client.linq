<Query Kind="Program">
  <Reference>&lt;RuntimeDirectory&gt;\System.Security.dll</Reference>
  <NuGetReference>Microsoft.Graph</NuGetReference>
  <NuGetReference Prerelease="true">Microsoft.Graph.Auth</NuGetReference>
  <NuGetReference>Microsoft.Graph.Core</NuGetReference>
  <NuGetReference>Microsoft.Identity.Client</NuGetReference>
  <NuGetReference>Newtonsoft.Json</NuGetReference>
  <Namespace>Microsoft.Graph</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>Microsoft.Graph.Auth</Namespace>
  <Namespace>Microsoft.Identity.Client</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

// https://docs.microsoft.com/en-us/graph/api/resources/notebook?view=graph-rest-1.0 is the raw JSON,
// which isn't what you really want.  The GraphClient gives you type safety.  The link does help
// you naviage the types, and is necessary if you use a language that does not have Graph SDK support.

// 1. Create an App ID in the Azure Portal.
//   a. Azure Active Directory
//   b. App Registrations
//   c. New Registration
//   d. Redirect URI (optional) - select Public client/native (mobile & desktop)
//   e. Create
//   f. Go back to App, and select API Permissions.
//   g. Add permissions for Notes (see below Notes.Create, Notes.Read...)
//   h. Go back to App, and select Authentication.
//   i. /!\ Set *Allow public client flows* Yes. /!\
// 2. Use the App ID's GUID.
// 3. Party.

// Useful links if you want or need more background on Graph and authentication.
//
// https://docs.microsoft.com/en-us/graph/overview
//
// https://docs.microsoft.com/en-us/graph/sdks/choose-authentication-providers?tabs=CS
// https://docs.microsoft.com/en-us/azure/active-directory/develop/tutorial-v2-windows-desktop
// https://docs.microsoft.com/en-us/azure/active-directory/develop/scenario-desktop-acquire-token?tabs=dotnet
// https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/1234
//
// If you don't want to go through the device flow every single time you need to cache the token.
// The code is below, but this is where I learned.
//
// https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/AcquireTokenSilentAsync-using-a-cached-token
// https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache

class MyAuthProvider : IAuthenticationProvider {
    private readonly IPublicClientApplication app;
    private readonly IEnumerable<string> scopes;
    public MyAuthProvider(IPublicClientApplication app, IEnumerable<string> scopes) {
        this.app = app;
        this.scopes = scopes;
    }

    public async Task AuthenticateRequestAsync(HttpRequestMessage request) {
        var accounts = await app.GetAccountsAsync();
        AuthenticationResult result = await app.AcquireTokenSilent(scopes, accounts.FirstOrDefault()).ExecuteAsync();
        if (result == null) {
            result = await app.AcquireTokenWithDeviceCode(scopes, x => {
                Console.WriteLine(x.Message);
                return Task.FromResult(0);
            }).ExecuteAsync();
        }
        
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
    }
}

void Main() {
    var scopes = new string[] {
        "user.read",
        "Notes.Create",
        "Notes.Read",
        "Notes.Read.All",
        "Notes.ReadWrite",
        "Notes.ReadWrite.All",
    };
    
    var app = PublicClientApplicationBuilder.Create(
        "6eb89130-96ad-49b1-aab4-626ed82bcdff")
        .WithAuthority("https://login.microsoftonline.com/consumers")
        .WithTenantId("common")
        .Build();

#region Caching
    var cachedTokenPath = @"graph.token";
    app.UserTokenCache.SetBeforeAccess(x => {
        if (System.IO.File.Exists(cachedTokenPath)) {
            x.TokenCache.DeserializeMsalV3(
                ProtectedData.Unprotect(
                    System.IO.File.ReadAllBytes(cachedTokenPath), null, DataProtectionScope.CurrentUser));
        }
    });
    app.UserTokenCache.SetAfterAccess(x => {
        if (x.HasStateChanged) {
            System.IO.File.WriteAllBytes(
                cachedTokenPath,
                ProtectedData.Protect(x.TokenCache.SerializeMsalV3(), null, DataProtectionScope.CurrentUser));
        }
    });
#endregion Caching

    var client = new GraphServiceClient(new MyAuthProvider(app, scopes));
    client.Me.Onenote.Notebooks.Request().GetAsync().GetAwaiter().GetResult()
        .Select(x => new {
            User = x.CreatedBy.User.DisplayName,
            x.DisplayName,
            x.CreatedDateTime,
        })
        .Dump();
}
