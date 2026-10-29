using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage;
using ToeicCoachAI.Repository.Data;
using ToeicCoachAI.Repository.Interfaces;

namespace ToeicCoachAI.Repository.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ToeicCoachAiDbContext _context;
        private readonly ConcurrentDictionary<Type, object> _repositories = new();
        private IDbContextTransaction? _currentTransaction;
        private IUserRepository? _users;

        public UnitOfWork(ToeicCoachAiDbContext context)
        {
            _context = context;
        }

        public IUserRepository Users => _users ??= new UserRepository(_context);

        public IGenericRepository<T> Repository<T>() where T : class
        {
            return (IGenericRepository<T>)_repositories.GetOrAdd(
                typeof(T),
                _ => new GenericRepository<T>(_context));
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction != null)
            {
                return;
            }

            _currentTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
            {
                return;
            }

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
                await _currentTransaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await RollbackTransactionAsync(cancellationToken);
                throw;
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
        {
            if (_currentTransaction == null)
            {
                return;
            }

            try
            {
                await _currentTransaction.RollbackAsync(cancellationToken);
            }
            finally
            {
                await _currentTransaction.DisposeAsync();
                _currentTransaction = null;
            }
        }

        public void Dispose()
        {
            _currentTransaction?.Dispose();
            _context.Dispose();
        }
    }
}
