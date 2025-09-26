using VendasService.Models;

namespace VendasService.Services
{
    public interface ITokenService
    {
        string GenerateToken(string username);
        LoginResponse GenerateLoginResponse(string username);
    }
}