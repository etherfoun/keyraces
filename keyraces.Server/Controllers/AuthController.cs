using Microsoft.AspNetCore.Mvc;
using keyraces.Server.Dtos;
using keyraces.Core.Interfaces;
using Microsoft.AspNetCore.Identity;
using keyraces.Core.Entities;
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

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IUserService userService,
            IUserProfileService profileService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _userService = userService;
            _profileService = profileService;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            var newUserId = await _userService.RegisterAsync(
                dto.Name, dto.Email, dto.Password);

            var identityUser = await _userManager.FindByIdAsync(newUserId);
            if (identityUser == null)
            {
                return BadRequest("User registration failed");
            }

            await _profileService.CreateProfileAsync(newUserId, dto.Name);
            await _signInManager.SignInAsync(identityUser, isPersistent: true);

            return CreatedAtAction(
                actionName: nameof(Register),
                routeValues: new { id = newUserId },
                value: new { id = newUserId }
            );
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            var result = await _signInManager.PasswordSignInAsync(
                dto.Email, dto.Password,
                isPersistent: true,
                lockoutOnFailure: false);

            return result.Succeeded
                ? Ok()
                : Unauthorized();
        }

        [HttpGet("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/login");
        }
    }
}
