using Microsoft.AspNetCore.Mvc;
using keyraces.Server.Dtos;
using keyraces.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using keyraces.Core.Models;
using Microsoft.AspNetCore.Authorization;

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
            _logger.LogInformation("Register attempt for email: {Email}", dto.Email);

            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists.", dto.Email);
                return Conflict(new { success = false, message = "Email already exists." });
            }

            var user = new IdentityUser { UserName = dto.Name, Email = dto.Email };
            var result = await _userManager.CreateAsync(user, dto.Password);

            if (!result.Succeeded)
            {
                _logger.LogWarning("Registration failed for {Email}: {Errors}", dto.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new { success = false, errors = result.Errors.Select(e => e.Description) });
            }

            await _userManager.AddToRoleAsync(user, "User");
            _logger.LogInformation("User {Email} assigned 'User' role.", dto.Email);

            var profile = await _profileService.GetOrCreateAsync(user.Id, dto.Name);
            if (profile == null)
            {
                _logger.LogError("Failed to create or retrieve profile for user {UserId} during registration.", user.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "User registered but profile creation failed." });
            }
            _logger.LogInformation("User profile created/retrieved for {Email}, Profile ID: {ProfileId}", dto.Email, profile.Id);

            var claimResult = await _userManager.AddClaimAsync(user, new Claim("user_profile_id", profile.Id.ToString()));
            if (!claimResult.Succeeded)
            {
                _logger.LogWarning("Failed to add user_profile_id claim for user {UserId}", user.Id);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User {Email} signed in after registration (session cookie set).", dto.Email);

            var authResponse = await _tokenService.GenerateTokensAsync(user, profile.Name);
            if (authResponse == null || !authResponse.Success)
            {
                _logger.LogWarning("Token generation failed for {Email} after registration: {Message}", dto.Email, authResponse?.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = authResponse?.Message ?? "Token generation failed." });
            }
            _logger.LogInformation("JWT tokens generated for user {Email}", dto.Email);

            Response.Cookies.Append("X-Refresh-Token", authResponse.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7) 
            });

            return Ok(authResponse); 
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            _logger.LogInformation("Login attempt for email: {Email}", dto.Email);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                _logger.LogWarning("Login failed for {Email}: User not found.", dto.Email);
                return Unauthorized(new { success = false, message = "Invalid email or password." });
            }

            var passwordCheckResult = await _userManager.CheckPasswordAsync(user, dto.Password);
            if (!passwordCheckResult)
            {
                _logger.LogWarning("Login failed for {Email}: Invalid password (checked by UserManager).", dto.Email);
                return Unauthorized(new { success = false, message = "Invalid email or password." });
            }

            var signInResult = await _signInManager.PasswordSignInAsync(user, dto.Password, isPersistent: false, lockoutOnFailure: true);

            if (!signInResult.Succeeded)
            {
                _logger.LogWarning("Login failed for {Email} via SignInManager. Result: {SignInResult}", dto.Email, signInResult.ToString());
                if (signInResult.IsLockedOut)
                {
                    return StatusCode(StatusCodes.Status423Locked, new { success = false, message = "Account locked out." });
                }
                return Unauthorized(new { success = false, message = "Invalid login attempt." });
            }
            _logger.LogInformation("User {Email} logged in successfully (session cookie set by SignInManager).", dto.Email);

            var profile = await _profileService.GetOrCreateAsync(user.Id, user.UserName ?? dto.Email);
            if (profile == null)
            {
                _logger.LogError("Failed to create or retrieve profile for user {UserId} during login.", user.Id);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = "User logged in but profile retrieval/creation failed." });
            }
            _logger.LogInformation("User profile retrieved/created: {ProfileName}, Profile ID: {ProfileId}", profile.Name, profile.Id);

            var existingProfileClaim = (await _userManager.GetClaimsAsync(user)).FirstOrDefault(c => c.Type == "user_profile_id");
            if (existingProfileClaim != null && existingProfileClaim.Value != profile.Id.ToString())
            {
                await _userManager.ReplaceClaimAsync(user, existingProfileClaim, new Claim("user_profile_id", profile.Id.ToString()));
            }
            else if (existingProfileClaim == null)
            {
                await _userManager.AddClaimAsync(user, new Claim("user_profile_id", profile.Id.ToString()));
            }

            var authResponse = await _tokenService.GenerateTokensAsync(user, profile.Name);
            if (authResponse == null || !authResponse.Success)
            {
                _logger.LogWarning("Token generation failed for {Email} after login: {Message}", authResponse?.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { success = false, message = authResponse?.Message ?? "Token generation failed." });
            }
            _logger.LogInformation("JWT tokens generated for user {Email}", dto.Email);

            Response.Cookies.Append("X-Refresh-Token", authResponse.RefreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(authResponse);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name ?? "Unknown user";
            _logger.LogInformation("User logout attempt: {UserName}", userName);

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User {UserName} signed out (session cookie removed).", userName);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await _tokenService.RevokeTokenAsync(userId);
                _logger.LogInformation("Revoked refresh tokens for user {UserId}", userId);
            }

            Response.Cookies.Delete("X-Refresh-Token", new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict
            });

            return Ok(new { success = true, message = "Logout successful." });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] TokenRequest request) 
        {
            _logger.LogInformation("Token refresh request received.");

            if (string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.RefreshToken))
            {
                _logger.LogWarning("Token refresh failed: Token or RefreshToken is missing.");
                return BadRequest(new { success = false, message = "Token and RefreshToken are required." });
            }

            var authResponse = await _tokenService.RefreshTokenAsync(request.Token, request.RefreshToken);

            if (authResponse == null || !authResponse.Success)
            {
                _logger.LogWarning("Token refresh failed: {Message}", authResponse?.Message ?? "Token service returned unsuccessful or null response.");
                return Unauthorized(new { success = false, message = authResponse?.Message ?? "Invalid token or refresh token." });
            }

            if (!string.IsNullOrEmpty(authResponse.RefreshToken))
            {
                Response.Cookies.Append("X-Refresh-Token", authResponse.RefreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(7)
                });
            }

            _logger.LogInformation("Token refreshed successfully for user associated with the token.");
            return Ok(authResponse);
        }

        [HttpGet("status")]
        [Authorize]
        public IActionResult Status()
        {
            var isAuthenticated = User.Identity?.IsAuthenticated ?? false;
            _logger.LogInformation("Auth status check: User {UserName}, IsAuthenticated: {IsAuthenticated}", User.Identity?.Name ?? "N/A", isAuthenticated);

            if (isAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email) ?? User.FindFirstValue(ClaimTypes.Name);
                var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                var userProfileIdClaim = User.FindFirst("user_profile_id")?.Value;

                return Ok(new
                {
                    isAuthenticated = true,
                    userId,
                    email,
                    roles,
                    userProfileId = userProfileIdClaim
                });
            }

            _logger.LogWarning("Auth status check: Reached code that should be inaccessible if [Authorize] is working.");
            return Unauthorized(new { isAuthenticated = false, message = "Not authenticated." });
        }
    }
}
