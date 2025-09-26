using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using VendasService.Models;

namespace VendasService.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(string username)
        {
            var key = _configuration["Jwt:Key"] ?? "ChaveSuperSecretaParaJWTTokenSeguro@2025!";
            var issuer = _configuration["Jwt:Issuer"] ?? "VendasAPI";
            var audience = _configuration["Jwt:Audience"] ?? "VendasAPI";

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim("username", username)
            };

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(2),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public LoginResponse GenerateLoginResponse(string username)
        {
            var token = GenerateToken(username);
            return new LoginResponse
            {
                Token = token,
                Expiration = DateTime.Now.AddHours(2),
                Username = username
            };
        }
    }
}