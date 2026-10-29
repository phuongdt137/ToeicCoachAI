using Microsoft.EntityFrameworkCore;
using ToeicCoachAI.Model.Entities;
using ToeicCoachAI.Repository.Data;
using ToeicCoachAI.Repository.Interfaces;

namespace ToeicCoachAI.Repository.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(ToeicCoachAiDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            return await _dbSet.FirstOrDefaultAsync(
                u => u.Email.ToLower() == normalizedEmail,
                cancellationToken);
        }

        public async Task<bool> SyncGuestSessionToUserAsync(string anonymousToken, Guid userId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(anonymousToken))
            {
                return false;
            }

            var guestSession = await _context.GuestSessions
                .Include(g => g.TestSession)
                .FirstOrDefaultAsync(
                    g => g.AnonymousToken == anonymousToken && g.ClaimedByUserId == null,
                    cancellationToken);

            if (guestSession == null)
            {
                return false;
            }

            guestSession.ClaimedByUserId = userId;

            if (guestSession.TestSession != null && guestSession.TestSession.UserId == null)
            {
                guestSession.TestSession.UserId = userId;
            }

            var guestAttempts = await _context.UserAttempts
                .Where(a => a.SessionId == guestSession.TestSessionId && a.UserId == null)
                .ToListAsync(cancellationToken);

            foreach (var attempt in guestAttempts)
            {
                attempt.UserId = userId;
            }

            return true;
        }
    }
}
