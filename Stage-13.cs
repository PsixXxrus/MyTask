using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private const string JwtKey = "super_secret_key_12345"; // ❗ Храни в секрете
    private const string Issuer = "MyApp";
    private const string Audience = "MyAppClient";

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
            new Claim(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.Add(expiresIn),
            signingCredentials: creds);

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
