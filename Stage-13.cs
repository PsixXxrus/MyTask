using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class JwtAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ProtectedSessionStorage _sessionStorage;
    private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
    private string? _cachedToken;

    public JwtAuthenticationStateProvider(ProtectedSessionStorage sessionStorage)
    {
        _sessionStorage = sessionStorage;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            if (_cachedToken == null)
            {
                var tokenResult = await _sessionStorage.GetAsync<string>("accessToken");
                if (!tokenResult.Success || string.IsNullOrWhiteSpace(tokenResult.Value))
                    return new AuthenticationState(_anonymous);

                _cachedToken = tokenResult.Value;
            }

            var identity = ParseClaimsFromJwt(_cachedToken);
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch
        {
            return new AuthenticationState(_anonymous);
        }
    }

    public async Task MarkUserAsAuthenticated(string token)
    {
        _cachedToken = token;
        await _sessionStorage.SetAsync("accessToken", token);
        var identity = ParseClaimsFromJwt(token);
        var user = new ClaimsPrincipal(identity);
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    public async Task MarkUserAsLoggedOut()
    {
        _cachedToken = null;
        await _sessionStorage.DeleteAsync("accessToken");
        var user = _anonymous;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }

    private ClaimsIdentity ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(jwt);

        var claims = token.Claims.ToList();

        return new ClaimsIdentity(claims, "jwt");
    }
}