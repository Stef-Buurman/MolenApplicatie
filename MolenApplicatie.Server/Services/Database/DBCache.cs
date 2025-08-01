using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Interfaces;
using System.Linq.Expressions;

namespace MolenApplicatie.Server.Services.Database
{
    public static class CacheData<TEntity>
    {
        public static Dictionary<string, DateTime> Times = new Dictionary<string, DateTime>();
        public static Dictionary<string, List<TEntity>> Data = new Dictionary<string, List<TEntity>>();
    }

    public class DBCache<TEntity> where TEntity : class, DefaultModel
    {
        private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private List<TEntity> _cachedData = new List<TEntity>();
        private DateTime _lastRefreshTime = DateTime.MinValue;
        private readonly TimeSpan _cacheDuration = TimeSpan.FromMinutes(30);
        private readonly Func<Task<List<TEntity>>> _getAllAsync;
        private readonly string _cacheKey;

        public DBCache(Func<Task<List<TEntity>>> getAllAsync)
        {
            _getAllAsync = getAllAsync;
            _cacheKey = typeof(TEntity).Name;

            if (CacheData<TEntity>.Data.TryGetValue(_cacheKey, out var globalData))
            {
                _cachedData = globalData.Cast<TEntity>().ToList();
                _lastRefreshTime = CacheData<TEntity>.Times.TryGetValue(_cacheKey, out var timestamp)
                    ? timestamp
                    : DateTime.MinValue;
            }
        }

        private bool IsValidCache() => (DateTime.UtcNow - _lastRefreshTime) < _cacheDuration && _cachedData.Any();

        public async Task<List<TEntity>> GetAllAsync()
        {
            if (IsValidCache())
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
                    CacheData<TEntity>.Data[_cacheKey] = _cachedData.ToList();
                    CacheData<TEntity>.Times[_cacheKey] = _lastRefreshTime;
                }
            }
            finally
            {
                _lock.Release();
            }

            return _cachedData;
        }

        public TEntity? GetByIdAsync(Guid id)
        {
            if (!IsValidCache()) return null;
            return _cachedData.FirstOrDefault(e => e.Id == id);
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
            existing = null;
            if (!IsValidCache()) return false;
            existing = _cachedData.FirstOrDefault(predicate.Compile());
            return existing != null;
        }

        public void Invalidate()
        {
            _lastRefreshTime = DateTime.MinValue;
            CacheData<TEntity>.Times[_cacheKey] = _lastRefreshTime;
        }
    }
}