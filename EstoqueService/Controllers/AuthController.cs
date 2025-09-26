using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EstoqueService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            // Autenticação simples para testes
            if (request.Username != "admin" || request.Password != "123")
                return Unauthorized();

            // Claims do usuário
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, request.Username),
                new Claim(ClaimTypes.Role, "Admin") // Exemplo de role
            };

            // Chave secreta (deve ser igual à usada na configuração JWT)
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ChaveSuperSecretaParaJWTTokenSeguro@2025!"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Gerar token JWT
            var token = new JwtSecurityToken(
                issuer: "EstoqueAPI",
                audience: "EstoqueAPI",
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds);

            return Ok(new
            {
                token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }
    }

    public class LoginRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
    }
}
