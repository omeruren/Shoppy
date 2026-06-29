using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Shoppy.Business.Options;
using Shoppy.Entity.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shoppy.Business.Auth;

public sealed class JwtProvider(IOptions<JwtOptions> _options)
{

    public string CreateToken(User user)
    {
        string secretKey = _options.Value.SecretKey;

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));

        var signinCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        JwtSecurityToken jwtToken = new(
            issuer: _options.Value.Issuer,
            audience: _options.Value.Audience,
            claims: new List<Claim>
            {
                new("userId",user.Id.ToString()),
                new("userName",user.UserName!),
                new("fullName",user.FullName),
                new("email",user.Email!)
            },
            notBefore: DateTime.Now,
            expires: DateTime.Now.AddHours(1),
            signingCredentials: signinCredentials
            );

        JwtSecurityTokenHandler handler = new();
        var token = handler.WriteToken(jwtToken);

        return token;
    }
}

