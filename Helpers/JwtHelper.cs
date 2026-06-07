using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace DeviceMaintenanceSystem.Helpers;

public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 1440;
}

public interface IJwtHelper
{
    string GenerateToken(int userId, string username, string role);
    int? ValidateToken(string token);
}

public class JwtHelper : IJwtHelper
{
    private readonly JwtSettings _settings;

    public JwtHelper(JwtSettings settings)
    {
        _settings = settings;
    }

    public string GenerateToken(int userId, string username, string role)
    {
        var key = Encoding.UTF8.GetBytes(_settings.SecretKey);
        var credentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public int? ValidateToken(string token)
    {
        var key = Encoding.UTF8.GetBytes(_settings.SecretKey);
        var handler = new JwtSecurityTokenHandler();

        try
        {
            var claimsPrincipal = handler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _settings.Issuer,
                ValidAudience = _settings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key)
            }, out _);

            var userIdClaim = claimsPrincipal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            {
                return userId;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }
}
