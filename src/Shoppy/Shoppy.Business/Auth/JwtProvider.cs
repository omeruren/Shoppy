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

    public LoginResponseDto CreateToken(User user, List<string> roles)
    {
        string secretKey = _options.Value.SecretKey;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var signinCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);


        var claims = new List<Claim>()
        {
            new(ClaimTypes.NameIdentifier.ToString(),user.Id.ToString()),
            new("userName",user.UserName!),
            new("fullName",user.FirstName),
            new("email",user.Email!),
        };


        foreach (var role in roles)
        {
            var claim = new Claim("role", role);
            claims.Add(claim);

        }

        var expiresAt = DateTimeOffset.UtcNow.AddHours(1);

        JwtSecurityToken jwtToken = new(
            issuer: _options.Value.Issuer,
            audience: _options.Value.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signinCredentials
            );



        JwtSecurityTokenHandler handler = new();
        var accessToken = handler.WriteToken(jwtToken);

        var refreshToken = GenerateRefreshToken();

        return new LoginResponseDto(accessToken, refreshToken, expiresAt);
    }

    public static string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();

        rng.GetBytes(randomBytes);

        return Convert.ToBase64String(randomBytes);
    }
}

