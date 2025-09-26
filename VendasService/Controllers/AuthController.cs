using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace VendasService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Autenticação simples para testes (substituir por autenticação real)
                if (request.Username != "admin" || request.Password != "123")
                {
                    _logger.LogWarning("Tentativa de login falhada para usuário: {Username}", request.Username);
                    return Unauthorized(new { message = "Usuário ou senha inválidos" });
                }

                // Claims do usuário
                var claims = new[]
                {
                    new Claim(ClaimTypes.Name, request.Username),
                    new Claim(ClaimTypes.Role, "Admin"),
                    new Claim("ClienteId", request.Username) // Para rastreamento do cliente
                };

                // Chave secreta (deve ser igual à usada na configuração JWT)
                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"] ?? "ChaveSuperSecretaParaJWTTokenSeguro@2025!"));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                // Gerar token JWT
                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddHours(1),
                    signingCredentials: creds);

                _logger.LogInformation("Login realizado com sucesso para usuário: {Username}", request.Username);

                return Ok(new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(token),
                    expiration = token.ValidTo,
                    user = new
                    {
                        username = request.Username,
                        role = "Admin"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o processo de login");
                return StatusCode(500, "Erro interno do servidor");
            }
        }

        [HttpPost("refresh")]
        public IActionResult RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                // Implementação básica de refresh token
                // Em produção, implementar com refresh tokens seguros
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(
                    _configuration["Jwt:Key"] ?? "ChaveSuperSecretaParaJWTTokenSeguro@2025!");

                try
                {
                    tokenHandler.ValidateToken(request.Token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = _configuration["Jwt:Issuer"],
                        ValidateAudience = true,
                        ValidAudience = _configuration["Jwt:Audience"],
                        ValidateLifetime = false, // Não validar expiração para refresh
                        ClockSkew = TimeSpan.Zero
                    }, out SecurityToken validatedToken);

                    var jwtToken = (JwtSecurityToken)validatedToken;
                    var username = jwtToken.Claims.First(x => x.Type == ClaimTypes.Name).Value;

                    // Gerar novo token
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, username),
                        new Claim(ClaimTypes.Role, "Admin"),
                        new Claim("ClienteId", username)
                    };

                    var newKey = new SymmetricSecurityKey(key);
                    var creds = new SigningCredentials(newKey, SecurityAlgorithms.HmacSha256);

                    var newToken = new JwtSecurityToken(
                        issuer: _configuration["Jwt:Issuer"],
                        audience: _configuration["Jwt:Audience"],
                        claims: claims,
                        expires: DateTime.Now.AddHours(1),
                        signingCredentials: creds);

                    return Ok(new
                    {
                        token = tokenHandler.WriteToken(newToken),
                        expiration = newToken.ValidTo
                    });
                }
                catch
                {
                    return Unauthorized(new { message = "Token inválido" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer refresh do token");
                return StatusCode(500, "Erro interno do servidor");
            }
        }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "Username é obrigatório")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password é obrigatório")]
        public string Password { get; set; } = string.Empty;
    }

    public class RefreshTokenRequest
    {
        [Required(ErrorMessage = "Token é obrigatório")]
        public string Token { get; set; } = string.Empty;
    }
}