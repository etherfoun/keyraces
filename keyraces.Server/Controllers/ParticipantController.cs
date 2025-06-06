﻿using Microsoft.AspNetCore.Mvc;
using keyraces.Core.Entities;
using keyraces.Core.Interfaces;
using keyraces.Server.Dtos;

namespace keyraces.Server.Controllers
{
    [ApiController]
    [Route("api/competitions/{competitionId}/participants")]
    public class ParticipantController : ControllerBase
    {
        private readonly ICompetitionParticipantService _service;
        public ParticipantController(ICompetitionParticipantService service) => _service = service;

        [HttpPost("join")]
        public async Task<IActionResult> Join(int competitionId, JoinDto dto) // Assume JoinDto.UserId is string
        {
            // CRITICAL: _service.JoinAsync MUST be updated to accept string userId
            // If it still expects int, this line will cause a runtime error or fail to find user.
            // The CS1503 error means it currently expects int.
            await _service.JoinAsync(competitionId, dto.UserId);
            return NoContent();
        }

        [HttpPost("begin")]
        public async Task<IActionResult> Begin(int competitionId, UserDto dto) // UserDto.UserId is string
        {
            // CRITICAL: _service.BeginTypingAsync MUST be updated to accept string userId
            await _service.BeginTypingAsync(competitionId, dto.Id);
            return NoContent();
        }

        [HttpPost("complete")]
        public async Task<IActionResult> Complete(int competitionId, CompleteDto dto) // Assume CompleteDto.UserId is string
        {
            // CRITICAL: _service.CompleteTypingAsync MUST be updated to accept string userId
            await _service.CompleteTypingAsync(competitionId, dto.UserId, dto.WPM, dto.Errors);
            return NoContent();
        }

        [HttpGet]
        public Task<IEnumerable<CompetitionParticipant>> List(int competitionId) =>
            _service.GetParticipantsAsync(competitionId);
    }
}
