using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Enums;
using MolenApplicatie.Server.Interfaces;
using MolenApplicatie.Server.Utils;
using System.Linq.Expressions;

namespace MolenApplicatie.Server.Services.Database
{
    public abstract class DBDefaultService<TEntity> where TEntity : class, DefaultModel, IEquatable<TEntity>
    {
        public readonly MolenDbContext _context;
        public readonly DbSet<TEntity> _dbSet;
        public readonly DBCache<TEntity> _cache;
        public DBDefaultService(MolenDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<TEntity>();
            _cache = new DBCache<TEntity>(_dbSet, GetAllAsync);
        }

        public virtual async Task<List<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<TEntity?> GetById(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<TEntity> Add(TEntity entity, CancellationToken token = default)
        {
            if (entity == null) return entity;

            if (Exists(entity, out TEntity? existingEntity))
            {
                return existingEntity!;
            }
            token.ThrowIfCancellationRequested();
            entity.Id = Guid.Empty;
            var addedEntityEntry = await _dbSet.AddAsync(entity, token);
            _cache.Add(addedEntityEntry.Entity);
            return addedEntityEntry.Entity;
        }

        public virtual async Task<TEntity> Update(TEntity entity, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (entity == null) return entity;

            token.ThrowIfCancellationRequested();

            var existingEntry = _context.ChangeTracker
                .Entries<TEntity>()
                .FirstOrDefault(e => e.Entity != null && e.Entity.Id == entity.Id);

            if (existingEntry != null)
            {
                existingEntry.State = EntityState.Detached;
            }

            _dbSet.Update(entity);
            _cache.Update(entity);
            await Task.CompletedTask;
            return entity;
        }

        public virtual async Task<List<TEntity>> UpdateRange(List<TEntity> entities, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (entities == null) return entities;

            var entitiesToUpdate = new List<TEntity>();

            var changeTrackerEntities = _context.ChangeTracker.Entries<TEntity>()
                            .ToDictionary(e => e.Entity.Id);

            var localEntities = _context.Set<TEntity>().Local
                            .ToDictionary(e => e.Id);

            foreach (var entity in entities)
            {
                token.ThrowIfCancellationRequested();
                if (Exists(entity, out TEntity? existingEntity))
                {
                    if (entity.Id == Guid.Empty && existingEntity.Id != Guid.Empty)
                    {
                        entity.Id = existingEntity.Id;
                    }

                    if (changeTrackerEntities.TryGetValue(entity.Id, out var trackedEntity))
                    {
                        trackedEntity.CurrentValues.SetValues(entity);
                    }
                    else
                    {
                        if (localEntities.TryGetValue(entity.Id, out var localEntity))
                        {
                            _context.Entry(localEntity).State = EntityState.Detached;
                        }

                        entitiesToUpdate.Add(entity);
                    }
                }
                else
                {
                    await Add(entity);
                }
            }

            foreach (var entity in entitiesToUpdate.DistinctBy(e => e.Id))
            {
                token.ThrowIfCancellationRequested();
                _context.Attach(entity);
                _context.Entry(entity).State = EntityState.Modified;
            }

            _cache.UpdateRange(entitiesToUpdate);

            return entities;
        }

        public virtual async Task<TEntity> AddOrUpdate(TEntity entity, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (entity == null) return entity;

            token.ThrowIfCancellationRequested();

            if (Exists(entity, out TEntity? existingEntity))
            {
                if (strat == UpdateStrategy.Ignore) return existingEntity;

                if (entity.Id == Guid.Empty && existingEntity != null && existingEntity.Id != Guid.Empty)
                {
                    entity.Id = existingEntity.Id;
                }

                if (strat == UpdateStrategy.Patch)
                {
                    PatchUtil.Patch(entity, existingEntity);
                }

                return await Update(entity, token, strat);
            }
            else
            {
                return await Add(entity, token);
            }
        }

        public virtual async Task<List<TEntity>> AddOrUpdateRange(List<TEntity> entities, CancellationToken token = default, UpdateStrategy strat = UpdateStrategy.Patch)
        {
            if (entities == null || entities.Count == 0)
                return entities;

            var all = await _cache.GetAllAsync();
            token.ThrowIfCancellationRequested();
            var entitiesToAdd = new List<TEntity>();
            var entitiesToUpdate = new List<TEntity>();

            if (ExistsRange(entities, out List<TEntity> existingEntities, out List<TEntity> newEntities, out List<TEntity> updatedEntities, false, token, strat))
            {
                entitiesToUpdate.AddRange(updatedEntities);
                entitiesToAdd.AddRange(newEntities);
            }
            else
            {
                entitiesToAdd.AddRange(entities);
            }

            await AddRangeAsync(entitiesToAdd, token);
            await UpdateRange(entitiesToUpdate, token, strat);

            return entitiesToAdd.Concat(entitiesToUpdate).ToList();
        }

        public virtual async Task AddRangeAsync(List<TEntity> entities, CancellationToken token = default)
        {
            if (entities == null || !entities.Any()) return;

            var localEntities = _context.Set<TEntity>().Local
                                        .ToDictionary(e => e.Id);

            foreach (var entity in entities)
            {
                token.ThrowIfCancellationRequested();
                if (localEntities.TryGetValue(entity.Id, out var trackedEntity))
                {
                    if (!ReferenceEquals(trackedEntity, entity))
                    {
                        _context.Entry(trackedEntity).State = EntityState.Detached;
                    }
                }
            }

            await _dbSet.AddRangeAsync(entities, token);
            _cache.AddRange(entities);
        }

        public virtual async Task Delete(TEntity entity)
        {
            if (entity == null) return;

            if (Exists(entity, out TEntity? existingEntity))
            {
                _dbSet.Remove(existingEntity);
                _cache.Remove(existingEntity);
            }
        }

        public virtual async Task DeleteRange(List<TEntity> entities)
        {
            if (entities == null) return;

            foreach (var entity in entities)
            {
                await Delete(entity);
            }
        }

        public abstract bool Exists(TEntity entity, out TEntity? existing);
        public abstract bool ExistsRange(List<TEntity> entities,
            out List<TEntity> matchingEntities,
            out List<TEntity> newEntities,
            out List<TEntity> updatedEntities,
            bool searchDB = true,
            CancellationToken token = default,
            UpdateStrategy strat = UpdateStrategy.Patch);

        public virtual bool Exists(Expression<Func<TEntity, bool>> predicate, out TEntity? existing)
        {
            bool exists = _cache.Exists(predicate, out existing);
            if (exists && existing?.Id != Guid.Empty) return true;
            existing = _context.ChangeTracker
                .Entries<TEntity>()
                .FirstOrDefault(e =>
                    (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Unchanged) &&
                    predicate.Compile().Invoke(e.Entity)
                )?.Entity;
            if (existing == null) existing = _dbSet.Local.SingleOrDefault(predicate.Compile());
            if (existing == null) existing = _dbSet.AsNoTracking().SingleOrDefault(predicate);
            if (existing != null)
            {
                _cache.Add(existing);
                return true;
            }
            return false;
        }

        public virtual bool ExistsRange(
            List<TEntity> entities,
            Func<TEntity, object> matchKeySelector,
            Func<TEntity, Expression<Func<TEntity, bool>>> predicateFactory,
            out List<TEntity> matchingEntities,
            out List<TEntity> newEntities,
            out List<TEntity> updatedEntities,
            bool searchDB = true,
            CancellationToken token = default,
            UpdateStrategy strat = UpdateStrategy.Patch)
        {
            matchingEntities = new List<TEntity>();
            newEntities = new List<TEntity>();
            updatedEntities = new List<TEntity>();
            var newEntitiesByKey = new Dictionary<object, TEntity>();
            if (entities == null || entities.Count == 0) return false;

            var keyToEntityMap = new Dictionary<object, TEntity>();
            var alreadyMatched = new HashSet<object>();

            foreach (var source in _cache.GetAllAsync().Result
                .Concat(_context.ChangeTracker.Entries<TEntity>()
                    .Where(e => e.State != EntityState.Deleted)
                    .Select(e => e.Entity))
                .Concat(_dbSet.Local))
            {
                token.ThrowIfCancellationRequested();
                var key = matchKeySelector(source);
                if (!keyToEntityMap.ContainsKey(key))
                    keyToEntityMap[key] = source;
            }

            var keysToQueryFromDb = new List<(object key, TEntity entity, Expression<Func<TEntity, bool>> predicate)>();

            foreach (var entity in entities)
            {
                token.ThrowIfCancellationRequested();
                var key = matchKeySelector(entity);

                if (keyToEntityMap.TryGetValue(key, out var match))
                {
                    matchingEntities.Add(match);
                    if (match.Id != Guid.Empty)
                    {
                        entity.Id = match.Id;
                        if (strat == UpdateStrategy.Ignore) continue;
                        if (strat == UpdateStrategy.Patch) PatchUtil.Patch(entity, match);

                        updatedEntities.Add(entity);
                    }
                }
                else
                {
                    newEntitiesByKey[key] = entity;
                    keysToQueryFromDb.Add((key, entity, predicateFactory(entity)));
                }
            }
            if (searchDB)
            {
                foreach (var (key, entity, predicate) in keysToQueryFromDb)
                {
                    token.ThrowIfCancellationRequested();
                    var match = _dbSet.AsNoTracking().FirstOrDefault(predicate);
                    if (match != null)
                    {
                        matchingEntities.Add(match);
                        if (match.Id != Guid.Empty)
                        {
                            entity.Id = match.Id;
                            if (strat == UpdateStrategy.Ignore) continue;
                            if (strat == UpdateStrategy.Patch) PatchUtil.Patch(entity, match);

                            updatedEntities.Add(entity);
                        }
                        _cache.Add(match);
                        if (newEntitiesByKey.TryGetValue(key, out var currMatch))
                        {
                            newEntitiesByKey.Remove(key);
                        }
                    }
                    else
                    {
                        if (!newEntitiesByKey.ContainsKey(key))
                        {
                            newEntitiesByKey[key] = entity;
                        }
                    }
                }
            }

            newEntities = newEntitiesByKey.Values.ToList();

            return matchingEntities.Count > 0;
        }
    }
}