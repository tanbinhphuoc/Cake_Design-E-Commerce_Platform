using Application.DTOs;
using Application.Services;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services
{
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly string _secretKey;
        private readonly int _expirationMinutes;
        private readonly string _issuer;
        private readonly string _audience;

        public JwtTokenGenerator(IConfiguration config)
        {
            _secretKey = config["Jwt:SecretKey"] ?? "YourSuperSecretKeyHereAtLeast32CharsLong!!!";
            _expirationMinutes = int.TryParse(config["Jwt:ExpirationMinutes"], out var m) ? m : 60;
            _issuer = config["Jwt:Issuer"] ?? "CakeDesignPlatform";
            _audience = config["Jwt:Audience"] ?? "CakeDesignPlatformUsers";
        }

        public JwtTokenResult GenerateToken(Account user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jti = Guid.NewGuid().ToString();
            var expires = DateTime.UtcNow.AddMinutes(_expirationMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, jti)
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                expires: expires,
                signingCredentials: credentials
            );

            return new JwtTokenResult
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                Jti = jti,
                ExpiresAtUtc = expires
            };
        }
    }
}
