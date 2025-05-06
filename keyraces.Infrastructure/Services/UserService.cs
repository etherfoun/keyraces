using keyraces.Core.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace keyraces.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IUserProfileService _userProfileService;

        public UserService(
            UserManager<IdentityUser> userManager,
            IUserProfileService userProfileService)
        {
            _userManager = userManager;
            _userProfileService = userProfileService;
        }

        public async Task<string> RegisterAsync(string name, string email, string password)
        {
            var user = new IdentityUser
            {
                UserName = name,
                Email = email
            };
            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
            {
                var errs = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException("User registration failed");
            }

            await _userProfileService.CreateProfileAsync(user.Id, user.UserName);

            return user.Id;
        }

        public async Task<bool> ValidateLoginAsync(string email, string password)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                return false;
            }

            return await _userManager.CheckPasswordAsync(user, password);
        }
    }
}
