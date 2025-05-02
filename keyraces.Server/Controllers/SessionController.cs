using Microsoft.AspNetCore.Mvc;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionController : ControllerBase
    {
        private readonly ITypingSessionService _service;
        public SessionController(ITypingSessionService service) => _service = service;

        [HttpPost("start")]
        public Task<TypingSession> Start(StartSessionDto dto) =>
            _service.StartSessionAsync(dto.UserId, dto.TextSnippetId);

        [HttpPost("{id}/complete")]
        public async Task<IActionResult> Complete(int id)
        {
            await _service.CompleteSessionAsync(id, DateTime.UtcNow);
            return NoContent();
        }

        [HttpGet("user/{userId}")]
        public Task<IEnumerable<TypingSession>> GetByUser(int userId) =>
            _service.GetByUserAsync(userId);
    }
}
