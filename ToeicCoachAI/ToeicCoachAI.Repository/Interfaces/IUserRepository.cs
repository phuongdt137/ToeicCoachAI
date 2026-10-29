using ToeicCoachAI.Model.Entities;

namespace ToeicCoachAI.Repository.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
        Task<bool> SyncGuestSessionToUserAsync(string anonymousToken, Guid userId, CancellationToken cancellationToken = default);
    }
}
