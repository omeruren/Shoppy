using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shoppy.Business.Auth.DataTransferObjects;
using Shoppy.Business.Options;
using Shoppy.Entity.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shoppy.Business.Auth;

public sealed class JwtProvider(IOptions<JwtOptions> _options)
{

    public LoginResponseDto CreateToken(User user)
    {
        string secretKey = _options.Value.SecretKey;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var signinCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken jwtToken = new(
            issuer: _options.Value.Issuer,
            audience: _options.Value.Audience,
            claims: new List<Claim>
            {
                new(ClaimTypes.NameIdentifier.ToString(),user.Id.ToString()),
                new("userName",user.UserName!),
                new("fullName",user.FullName),
                new("email",user.Email!)
            },
            notBefore: DateTime.Now,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: signinCredentials
            );

        JwtSecurityTokenHandler handler = new();
        var accessToken = handler.WriteToken(jwtToken);

        return new(accessToken);
    }
}

