using Microsoft.AspNetCore.Mvc;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionController : ControllerBase
    {
        private readonly ITypingSessionService _sessionService;
        private readonly ITypingStatisticService _statService;

        public SessionController(ITypingSessionService sessionService, ITypingStatisticService statService)
        {
            _sessionService = sessionService;
            _statService = statService;
        }


        // POST /api/session
        [HttpPost]
        public async Task<ActionResult<SessionDto>> Start([FromBody] StartSessionDto dto)
        {
            var session = await _sessionService.StartSessionAsync(dto.UserId, dto.TextSnippetId);

            var resultDto = new SessionDto(
                id: session.Id,
                startTime: session.StartTime
            );

            return CreatedAtAction(
                nameof(GetById),
                new { id = session.Id },
                resultDto
            );
        }

        // GET /api/session/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SessionDto>> GetById(int id)
        {
            var session = await _sessionService.GetByIdAsync(id);  // одиночный объект
            if (session == null)
                return NotFound();

            return Ok(new SessionDto(session.Id, session.StartTime));
        }

        // POST /api/session/{id}/complete
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> Complete(
            int id,
            [FromBody] CompleteDto dto)   
        {
            var endTime = DateTime.UtcNow;
            await _sessionService.CompleteSessionAsync(id, endTime);

            await _statService.CreateAsync(id, dto.WPM, dto.Errors);

            return NoContent();
        }

        // GET /api/session/user/{userProfileId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<SessionDto>>> GetByUser(int userId)
        {
            var list = await _sessionService.GetByUserAsync(userId);
            var dtos = list.Select(s => new SessionDto(s.Id, s.StartTime));
            return Ok(dtos);
        }
    }
}
