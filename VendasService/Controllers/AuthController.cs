using Microsoft.AspNetCore.Mvc;
using VendasService.Models;
using VendasService.Services;

namespace VendasService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(ITokenService tokenService, ILogger<AuthController> logger)
        {
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost("login")]
        public ActionResult<LoginResponse> Login([FromBody] LoginModel loginModel)
        {
            try
            {
                _logger.LogInformation("Tentativa de login para usuário: {Username}", loginModel.Username);
                
                if (IsValidUser(loginModel.Username, loginModel.Password))
                {
                    var response = _tokenService.GenerateLoginResponse(loginModel.Username);
                    
                    _logger.LogInformation("Login realizado com sucesso para usuário: {Username}", loginModel.Username);
                    
                    return Ok(response);
                }

                _logger.LogWarning("Credenciais inválidas para usuário: {Username}", loginModel.Username);
                return Unauthorized(new { message = "Credenciais inválidas" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante o login");
                return StatusCode(500, new { message = "Erro interno do servidor" });
            }
        }

        private bool IsValidUser(string username, string password)
        {
            _logger.LogInformation("Validando usuário: {Username}", username);
            
            var validUsers = new Dictionary<string, string>
            {
                { "admin", "admin123" },
                { "user", "user123" },
                { "test", "test123" },
                { "vendas", "vendas123" }
            };

            var isValid = validUsers.ContainsKey(username) && validUsers[username] == password;
            _logger.LogInformation("Resultado da validação para {Username}: {IsValid}", username, isValid);
            
            return isValid;
        }
    }
}