using System.Threading.Tasks;
using keyraces.Core.Models;
using Microsoft.AspNetCore.Identity;

namespace keyraces.Core.Interfaces
{
    public interface ITokenService
    {
        Task<AuthResponse> GenerateTokensAsync(IdentityUser user, string userName);
        Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken);
        Task<bool> ValidateTokenAsync(string token);
        Task RevokeTokenAsync(string userId);
    }
}
