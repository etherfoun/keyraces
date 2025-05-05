using keyraces.Core.Entities;

namespace keyraces.Core.Interfaces
{
    public interface ICompetitionRepository
    {
        Task<Competition> GetByIdAsync(int id);
        Task<IEnumerable<Competition>> ListAsync();
        Task AddAsync(Competition competition);
        Task UpdateAsync(Competition competition);
    }
}
