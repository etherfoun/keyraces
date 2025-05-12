using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Core.Models;
using keyraces.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace keyraces.Infrastructure.Services
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly AppDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public TokenService(
            IConfiguration configuration,
            AppDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _configuration = configuration;
            _context = context;
            _userManager = userManager;
        }

        public async Task<AuthResponse> GenerateTokensAsync(IdentityUser user, string userName)
        {
            var jwtTokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]);

            var claims = new List<Claim>
           {
               new Claim(JwtRegisteredClaimNames.Sub, user.Id),
               new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
               new Claim(JwtRegisteredClaimNames.Email, user.Email),
               new Claim("id", user.Id),
               new Claim("name", userName)
           };

            var userRoles = await _userManager.GetRolesAsync(user);
            foreach (var role in userRoles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(1),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature),
                Issuer = _configuration["JWT:Issuer"],
                Audience = _configuration["JWT:Audience"]
            };

            var token = jwtTokenHandler.CreateToken(tokenDescriptor);
            var jwtToken = jwtTokenHandler.WriteToken(token);

            var refreshToken = new RefreshToken
            {
                JwtId = token.Id,
                UserId = user.Id,
                CreationDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddDays(30), // Refresh token действителен 30 дней
                Invalidated = false,
                Used = false,
                Token = GenerateRefreshToken()
            };

            await _context.RefreshTokens.AddAsync(refreshToken);
            await _context.SaveChangesAsync();

            return new AuthResponse
            {
                Success = true,
                Token = jwtToken,
                RefreshToken = refreshToken.Token,
                Expiration = tokenDescriptor.Expires.Value,
                UserId = user.Id,
                UserName = userName
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken)
        {
            var validatedToken = GetPrincipalFromToken(token);

            if (validatedToken == null)
            {
                return new AuthResponse { Success = false, Message = "Invalid token" };
            }

            var expiryDateUnix = long.Parse(validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Exp).Value);
            var expiryDateTimeUtc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                .AddSeconds(expiryDateUnix);

            if (expiryDateTimeUtc > DateTime.UtcNow)
            {
                return new AuthResponse { Success = false, Message = "Token has not expired yet" };
            }

            var jti = validatedToken.Claims.Single(x => x.Type == JwtRegisteredClaimNames.Jti).Value;
            var storedRefreshToken = await _context.RefreshTokens.SingleOrDefaultAsync(x => x.Token == refreshToken);

            if (storedRefreshToken == null)
            {
                return new AuthResponse { Success = false, Message = "Refresh token does not exist" };
            }

            if (DateTime.UtcNow > storedRefreshToken.ExpiryDate)
            {
                return new AuthResponse { Success = false, Message = "Refresh token has expired" };
            }

            if (storedRefreshToken.Invalidated)
            {
                return new AuthResponse { Success = false, Message = "Refresh token has been invalidated" };
            }

            if (storedRefreshToken.Used)
            {
                return new AuthResponse { Success = false, Message = "Refresh token has been used" };
            }

            if (storedRefreshToken.JwtId != jti)
            {
                return new AuthResponse { Success = false, Message = "Refresh token does not match JWT" };
            }

            storedRefreshToken.Used = true;
            _context.RefreshTokens.Update(storedRefreshToken);
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(storedRefreshToken.UserId);

            if (user == null)
            {
                return new AuthResponse { Success = false, Message = "User not found" };
            }

            var userName = validatedToken.Claims.Single(x => x.Type == "name").Value;
            return await GenerateTokensAsync(user, userName);
        }

        public async Task<bool> ValidateTokenAsync(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]);

            try
            {
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = _configuration["JWT:Issuer"],
                    ValidAudience = _configuration["JWT:Audience"],
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task RevokeTokenAsync(string userId)
        {
            var refreshTokens = await _context.RefreshTokens
                .Where(r => r.UserId == userId && !r.Used && !r.Invalidated)
                .ToListAsync();

            foreach (var token in refreshTokens)
            {
                token.Invalidated = true;
            }

            _context.RefreshTokens.UpdateRange(refreshTokens);
            await _context.SaveChangesAsync();
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]);

            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    ClockSkew = TimeSpan.Zero
                }, out var validatedToken);

                if (!IsJwtWithValidSecurityAlgorithm(validatedToken))
                {
                    return null;
                }

                return principal;
            }
            catch
            {
                return null;
            }
        }

        private bool IsJwtWithValidSecurityAlgorithm(SecurityToken validatedToken)
        {
            return (validatedToken is JwtSecurityToken jwtSecurityToken) &&
                   jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256,
                       StringComparison.InvariantCultureIgnoreCase);
        }

        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
