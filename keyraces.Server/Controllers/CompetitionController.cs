using Microsoft.AspNetCore.Mvc;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompetitionController : ControllerBase
    {
        private readonly ICompetitionService _service;
        public CompetitionController(ICompetitionService service) => _service = service;

        [HttpGet]
        public Task<IEnumerable<Competition>> GetAll() => _service.GetAllAsync();

        [HttpGet("{id}")]
        public Task<Competition> Get(int id) => _service.GetByIdAsync(id);

        [HttpPost]
        public async Task<IActionResult> Create(CreateCompetitionDto dto)
        {
            var comp = await _service.CreateAsync(dto.Title, dto.TextSnippetId, dto.StartTime);
            return CreatedAtAction(nameof(Get), new { id = comp.Id }, comp);
        }

        [HttpPut("{id}/start")]
        public async Task<IActionResult> Start(int id)
        {
            await _service.StartAsync(id);
            return NoContent();
        }

        [HttpPut("{id}/finish")]
        public async Task<IActionResult> Finish(int id)
        {
            await _service.FinishAsync(id);
            return NoContent();
        }
    }
}
