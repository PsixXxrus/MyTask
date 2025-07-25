using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

public class TokenService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ProtectedSessionStorage _sessionStorage;
    private readonly JwtAuthenticationStateProvider _authProvider;
    private Timer? _refreshTimer;

    public TokenService(
        IHttpClientFactory clientFactory,
        ProtectedSessionStorage sessionStorage,
        JwtAuthenticationStateProvider authProvider)
    {
        _clientFactory = clientFactory;
        _sessionStorage = sessionStorage;
        _authProvider = authProvider;
    }

    public async Task InitializeAsync()
    {
        var tokenResult = await _sessionStorage.GetAsync<string>("accessToken");
        if (!tokenResult.Success || string.IsNullOrEmpty(tokenResult.Value))
            return;

        ScheduleRefresh(tokenResult.Value);
    }

    private void ScheduleRefresh(string accessToken)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = handler.ReadJwtToken(accessToken);
        var expires = token.ValidTo;

        var timeToExpiry = expires - DateTime.UtcNow;

        // Обновим за 1 минуту до истечения
        var refreshIn = timeToExpiry - TimeSpan.FromMinutes(1);

        if (refreshIn < TimeSpan.Zero)
            refreshIn = TimeSpan.Zero;

        _refreshTimer?.Dispose();
        _refreshTimer = new Timer(async _ => await RefreshTokenAsync(), null, refreshIn, Timeout.InfiniteTimeSpan);
    }

    private async Task RefreshTokenAsync()
    {
        var refreshTokenResult = await _sessionStorage.GetAsync<string>("refreshToken");
        if (!refreshTokenResult.Success || string.IsNullOrWhiteSpace(refreshTokenResult.Value))
            return;

        var client = _clientFactory.CreateClient("ServerAPI");

        var response = await client.PostAsJsonAsync("auth/refresh", new
        {
            RefreshToken = refreshTokenResult.Value
        });

        if (response.IsSuccessStatusCode)
        {
            var data = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
            if (data != null && data.TryGetValue("accessToken", out var newToken))
            {
                await _sessionStorage.SetAsync("accessToken", newToken);
                await _authProvider.MarkUserAsAuthenticated(newToken);
                ScheduleRefresh(newToken);
            }
        }
        else
        {
            await _authProvider.MarkUserAsLoggedOut(); // ❌ refresh не сработал — выходим
        }
    }
}