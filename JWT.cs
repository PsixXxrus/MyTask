namespace Server.Core.Authentication
{
	[ApiController]
	[Route("auth")]
	public class JWTAuthController : ControllerBase
	{
		private const string JwtKey = "BodyProjectJWT";
		private const string Issuer = "ServerBP";
		private const string Audience = "AppicationBP";

		[HttpPost("login")]
		public IActionResult Login([FromBody] LoginModel model)
		{
			// 🔐 Простейшая проверка (замени на БД)
			if (model.Login == "admin" && model.Password == "123")
			{
				var accessToken = GenerateToken(model.Login, TimeSpan.FromMinutes(5));     // 🔓 короткоживущий токен
				var refreshToken = GenerateToken(model.Login, TimeSpan.FromMinutes(60));   // 🔁 токен для обновления

				return Ok(new
				{
					accessToken,
					refreshToken
				});
			}

			return Unauthorized();
		}

		[HttpPost("refresh")]
		public IActionResult Refresh([FromBody] RefreshRequest req)
		{
			var handler = new JwtSecurityTokenHandler();

			try
			{
				var token = handler.ReadJwtToken(req.RefreshToken);
				if (token.ValidTo < DateTime.UtcNow)
					return Unauthorized();

				var username = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
				if (string.IsNullOrWhiteSpace(username))
					return Unauthorized();

				var newToken = GenerateToken(username, TimeSpan.FromMinutes(5));
				return Ok(new { accessToken = newToken });
			}
			catch
			{
				return Unauthorized();
			}
		}

		private string GenerateToken(string login, TimeSpan expiresIn)
		{
			var claims = new[]
			{
				new Claim(ClaimTypes.Name, login),
				new Claim(ClaimTypes.NameIdentifier, "1"),
				new Claim(ClaimTypes.Role, "admin")
			};

			var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
			var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

			var token = new JwtSecurityToken(
				issuer: Issuer,
				audience: Audience,
				claims: claims,
				expires: DateTime.UtcNow.Add(expiresIn)
				//signingCredentials: creds Убираем шифрование токена по HMACSHA256
				);

			return new JwtSecurityTokenHandler().WriteToken(token);
		}
	}

	public class LoginModel
	{
		public string Login { get; set; } = string.Empty;
		public string Password { get; set; } = string.Empty;
	}

	public class RefreshRequest
	{
		public string RefreshToken { get; set; } = string.Empty;
	}
}

namespace Server.Core.Authentication
{
    public class JWTAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        private string? _cachedToken;

        public JWTAuthenticationStateProvider(ProtectedSessionStorage sessionStorage) => _sessionStorage = sessionStorage;

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
}

namespace Server.Core.Authentication
{
    public class JWTService
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly JWTAuthenticationStateProvider _authProvider;
        private Timer? _refreshTimer;

        public JWTService(
            IHttpClientFactory clientFactory,
            ProtectedSessionStorage sessionStorage,
            JWTAuthenticationStateProvider authProvider)
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
                await _authProvider.MarkUserAsLoggedOut(); // refresh не сработал — выходим
            }
        }
    }
}

var jwtKey = "BodyProjectJWT"; // Используется ключ для подписи токенов

builder.Services.AddAuthentication(options =>
	{
		options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
		options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
	}) // Регистрация системы аутентификации на основе JWT(по умолчанию схема 'Bearer').
	.AddJwtBearer(options =>
	{ 
		options.SaveToken = true; // Позволяет сохранить токен внутри HttpContext(например чтобы его можно было получить из HttpContext.GetTokenAsync())
		options.TokenValidationParameters = new TokenValidationParameters // Включаем проверку издателя
		{
			ValidateIssuer = true, // Включает проверку issuer
            ValidIssuer = "ServerBP", // Указывает, какой именно issuer считается допустимымм. Он должен совпадать с тем что указывается при создании токена
			ValidateAudience = true,  // Включает проверку audience
			ValidAudience = "AppicationBP", // Указывает, какой именно audience(кто потребитель токена) и устанавливает допустимое значение
			ValidateIssuerSigningKey = true, // Влюкает проверку подписи токена, чтобы убедиться, что токен подделать нельзя
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), // Использует симметричный ключ (один и тот же на создание и проверку токена)
			ValidateLifetime = true, // Проверяет, не истёк ли срок жизни токена (exp)
			ClockSkew = TimeSpan.FromSeconds(300) // Позволяет компенсировать небольшие расхождения между сервером и клиентом
		};
	});
builder.Services.AddAuthorization(); // Добавляем базовую инфрастктуру авторизации ([Authorize], Police, роли ...)

builder.Services.AddScoped<JWTAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JWTAuthenticationStateProvider>());
builder.Services.AddScoped<ProtectedSessionStorage>();

builder.Services.AddScoped<JWTService>();
