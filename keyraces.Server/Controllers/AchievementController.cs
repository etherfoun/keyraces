using Microsoft.AspNetCore.Mvc;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AchievementController : ControllerBase
    {
        private readonly IAchievementService _achService;
        private readonly IUserAchievementService _uaService;

        public AchievementController(
            IAchievementService achService,
            IUserAchievementService uaService)
        {
            _achService = achService;
            _uaService = uaService;
        }

        [HttpGet]
        public Task<IEnumerable<Achievement>> GetAll() => _achService.ListAsync();

        [HttpPost]
        public async Task<IActionResult> Create(CreateAchievementDto dto)
        {
            var ach = await _achService.CreateAchievementAsync(dto.Name, dto.Description);
            return CreatedAtAction(null, new { id = ach.Id }, ach);
        }

        [HttpPost("{achievementId}/award/{userId}")]
        public async Task<IActionResult> Award(int achievementId, int userId)
        {
            await _uaService.AwardAsync(userId, achievementId);
            return NoContent();
        }

        [HttpGet("user/{userId}")]
        public Task<IEnumerable<UserAchievement>> GetUserAchievements(int userId) =>
            _uaService.GetUserAchievementsAsync(userId);
    }
}
