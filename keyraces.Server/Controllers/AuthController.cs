using Microsoft.AspNetCore.Mvc;
using keyraces.Server.Dtos;
using keyraces.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using keyraces.Core.Models;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserProfileService _profileService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IUserService userService,
            IUserProfileService profileService,
            ITokenService tokenService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userService = userService;
            _profileService = profileService;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            _logger.LogInformation($"Register attempt for email: {dto.Email}");

            var user = new IdentityUser { UserName = dto.Email, Email = dto.Email };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning($"Registration failed for {dto.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                return BadRequest(result.Errors);
            }

            var profile = await _profileService.GetOrCreateAsync(user.Id, dto.Name);
            _logger.LogInformation($"User profile created for {dto.Email}");

            await _signInManager.SignInAsync(user, isPersistent: true);
            _logger.LogInformation($"User {dto.Email} signed in after registration");

            var authResponse = await _tokenService.GenerateTokensAsync(user, profile.Name);
            _logger.LogInformation($"JWT tokens generated for user {dto.Email}");

            return Ok(authResponse);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            _logger.LogInformation($"Login attempt for email: {dto.Email}");

            var result = await _signInManager.PasswordSignInAsync(
                dto.Email, dto.Password,
                isPersistent: true,
                lockoutOnFailure: false);

            if (result.Succeeded)
            {
                _logger.LogInformation($"User {dto.Email} logged in successfully");

                var user = await _userManager.FindByEmailAsync(dto.Email);
                var profile = await _profileService.GetOrCreateAsync(user.Id, user.Email);

                _logger.LogInformation($"User profile retrieved: {profile?.Name ?? "Unknown"}");

                var authResponse = await _tokenService.GenerateTokensAsync(user, profile.Name);
                _logger.LogInformation($"JWT tokens generated for user {dto.Email}");

                return Ok(authResponse);
            }
            else
            {
                _logger.LogWarning($"Login failed for {dto.Email}. Result: {result}");
                return Unauthorized(new { success = false, message = "Invalid login attempt" });
            }
        }

        [HttpGet("logout")]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User logout");

            await _signInManager.SignOutAsync();

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    await _tokenService.RevokeTokenAsync(userId);
                    _logger.LogInformation($"Revoked refresh tokens for user {userId}");
                }
            }

            return Ok(new { success = true });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRequest request)
        {
            _logger.LogInformation("Token refresh request");

            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
            {
                _logger.LogWarning("Token refresh failed: Token or RefreshToken is missing");
                return BadRequest(new { success = false, message = "Token and RefreshToken are required" });
            }

            var authResponse = await _tokenService.RefreshTokenAsync(request.Token, request.RefreshToken);

            if (!authResponse.Success)
            {
                _logger.LogWarning($"Token refresh failed: {authResponse.Message}");
                return BadRequest(authResponse);
            }

            _logger.LogInformation("Token refreshed successfully");
            return Ok(authResponse);
        }

        [HttpGet("status")]
        public async Task<IActionResult> Status()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            _logger.LogInformation($"Auth status check: {(isAuthenticated ? "Authenticated" : "Not authenticated")}");

            if (isAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name);

                return Ok(new
                {
                    isAuthenticated = true,
                    userId,
                    email
                });
            }

            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
            {
                var token = authHeader.Substring("Bearer ".Length).Trim();
                var isValid = await _tokenService.ValidateTokenAsync(token);

                if (isValid)
                {
                    return Ok(new { isAuthenticated = true });
                }
            }

            return Ok(new { isAuthenticated = false });
        }
    }
}
