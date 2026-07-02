using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.Options;
using Shoppy.Entity.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Shoppy.Business.Auth;

public sealed class JwtProvider(IOptions<JwtOptions> _options)
{
    /// <summary>
    /// Creates a JWT access token + a secure refresh token.
    /// Embeds role claims AND permission claims so the API can enforce
    /// fine-grained access control without hitting the database on every request.
    /// </summary>
    public LoginResponseDto CreateToken(User user, List<string?> roles, List<string> permissions)
    {
        string secretKey = _options.Value.SecretKey;

        var securityKey       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var signinCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("userName",  user.UserName!),
            new("fullName",  user.FullName),
            new("email",     user.Email!),
        };

        // Add role claims
        foreach (var role in roles.Where(r => r is not null))
            claims.Add(new Claim("role", role!));

        // Add permission claims — these are what the PermissionAuthorizationHandler reads
        foreach (var permission in permissions)
            claims.Add(new Claim("permission", permission));

        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        var jwtToken = new JwtSecurityToken(
            issuer:             _options.Value.Issuer,
            audience:           _options.Value.Audience,
            claims:             claims,
            notBefore:          DateTime.UtcNow,
            expires:            expiresAt.UtcDateTime,
            signingCredentials: signinCredentials);

        var handler      = new JwtSecurityTokenHandler();
        var accessToken  = handler.WriteToken(jwtToken);
        var refreshToken = GenerateRefreshToken();

        return new LoginResponseDto(accessToken, refreshToken, expiresAt);
    }

    public static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng   = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
