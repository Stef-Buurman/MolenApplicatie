using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Interfaces;
using System.Linq.Expressions;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBCache<TEntity> where TEntity : class, DefaultModel
    {
        private readonly MolenDbContext _context;
        private readonly DbSet<TEntity> _dbSet;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private List<TEntity> _cachedData = new List<TEntity>();
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);

        public DBCache(MolenDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
            GetAllAsync().Wait();
        }

        public async Task<List<TEntity>> GetAllAsync()
        {
            if ((DateTime.UtcNow - _lastRefreshTime) < _cacheDuration && _cachedData.Any())
            {
                return _cachedData;
            }

            await _lock.WaitAsync();
            try
            {
                if ((DateTime.UtcNow - _lastRefreshTime) >= _cacheDuration || !_cachedData.Any())
                {
                    _cachedData = await _dbSet.AsNoTracking().ToListAsync();
                    _lastRefreshTime = DateTime.UtcNow;
                }
            }
            finally
            {
                _lock.Release();
            }

            return _cachedData;
        }

        public async Task<TEntity?> GetByIdAsync(int id)
        {
            var allData = await GetAllAsync();
            return allData.FirstOrDefault(e => e.Id == id);
        }

        public TEntity Add(TEntity entity)
        {
            if (entity == null) return entity;
            if (Exists(e => e.Equals(entity), out TEntity? existingEntity))
            {
                return existingEntity!;
            }
            _dbSet.Add(entity);
            _cachedData.Add(entity);
            return entity;
        }

        public List<TEntity> AddRange(List<TEntity> entities)
        {
            if (entities == null || !entities.Any()) return entities;
            var existingEntities = new List<TEntity>();
            foreach (var entity in entities)
            {
                if (Exists(e => e.Equals(entity), out TEntity? existingEntity))
                {
                    existingEntities.Add(existingEntity!);
                }
                else
                {
                    _dbSet.Add(entity);
                    _cachedData.Add(entity);
                }
            }
            return existingEntities;
        }

        public TEntity Update(TEntity entity)
        {
            if (entity == null) return entity;
            _cachedData.RemoveAll(e => e.Id == entity.Id);
            _cachedData.Add(entity);
            return entity;
        }

        public TEntity Remove(TEntity entity)
        {
            if (entity == null) return entity;
            _cachedData.RemoveAll(e => e.Id == entity.Id);
            return entity;
        }

        public bool Exists(Expression<Func<TEntity, bool>> predicate, out TEntity? existing)
        {
            existing = _cachedData.FirstOrDefault(predicate.Compile());
            return existing != null;
        }

        public void Invalidate()
        {
            _lastRefreshTime = DateTime.MinValue;
        }
    }
}