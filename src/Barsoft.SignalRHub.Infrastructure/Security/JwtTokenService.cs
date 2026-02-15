using Barsoft.SignalRHub.Application.Interfaces;
using Barsoft.SignalRHub.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Barsoft.SignalRHub.Infrastructure.Security;

/// <summary>
/// JWT token generation and validation service
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        _jwtSettings.Validate();

        _tokenHandler = new JwtSecurityTokenHandler();
        _validationParameters = CreateValidationParameters();
    }

    /// <summary>
    /// Generates JWT token for authenticated user
    /// </summary>
    public string GenerateToken(User user)
    {
        if (!user.Aktif)
            throw new InvalidOperationException("Cannot generate token for inactive user");

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            new Claim("userCode", user.UserCode),
            new Claim("userName", user.Description),
            new Claim("isAdmin", user.Admin.ToString().ToLower())
        };

        // Add branch IDs claim for multi-tenant filtering
        if (!string.IsNullOrWhiteSpace(user.SubeIds))
        {
            claims.Add(new Claim("subeIds", user.SubeIds));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return _tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Validates JWT token
    /// </summary>
    public bool ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            _tokenHandler.ValidateToken(token, _validationParameters, out _);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Extracts user ID from JWT token
    /// </summary>
    public int? GetUserIdFromToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out _);
            var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
                return userId;

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates token validation parameters
    /// </summary>
    private TokenValidationParameters CreateValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = _jwtSettings.Issuer,
            ValidAudience = _jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret)),
            ClockSkew = TimeSpan.Zero // Remove default 5 minute clock skew
        };
    }
}
