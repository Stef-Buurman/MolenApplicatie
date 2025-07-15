using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Interfaces;
using System.Linq.Expressions;

namespace MolenApplicatie.Server.Services.Database
{
    public class DBCache<TEntity> where TEntity : class, DefaultModel
    {
        private readonly DbSet<TEntity> _dbSet;
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private List<TEntity> _cachedData = new List<TEntity>();
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);
        private readonly Func<Task<List<TEntity>>> _getAllAsync;

        public DBCache(DbSet<TEntity> dbSet, Func<Task<List<TEntity>>> getAllAsync)
        {
            _dbSet = dbSet;
            _getAllAsync = getAllAsync;
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
                    _cachedData = await _getAllAsync();
                    _lastRefreshTime = DateTime.UtcNow;
                }
            }
            finally
            {
                _lock.Release();
            }

            return _cachedData;
        }

        public async Task<TEntity?> GetByIdAsync(Guid id)
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
                    _cachedData.Add(entity);
                }
            }
            return existingEntities;
        }

        public TEntity Update(TEntity entity)
        {
            if (entity == null) return entity;
            Remove(entity);
            _cachedData.Add(entity);
            return entity;
        }

        public List<TEntity> UpdateRange(List<TEntity> entities)
        {
            if (entities == null || !entities.Any()) return entities;
            var updatedEntities = new List<TEntity>();
            foreach (var entity in entities)
            {
                Remove(entity);
                _cachedData.Add(entity);
                updatedEntities.Add(entity);
            }
            return updatedEntities;
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