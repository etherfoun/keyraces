using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ICompetitionService
    {
        Task<Competition> CreateAsync(string title, int snippetId, DateTime startTime);
        Task StartAsync(int competitionId);
        Task FinishAsync(int competitionId);
        Task<IEnumerable<Competition>> GetAllAsync();
        Task<Competition> GetByIdAsync(int competitionId);
    }
}
