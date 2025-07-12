using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Interfaces;
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
            _cache = new DBCache<TEntity>(context);
            _dbSet = _context.Set<TEntity>();
        }

        public virtual List<TEntity> GetAll()
        {
            return _dbSet.ToList();
        }

        public virtual async Task<TEntity?> GetById(Guid id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<TEntity> Add(TEntity entity)
        {
            if (entity == null) return entity;

            if (Exists(entity, out TEntity? existingEntity))
            {
                return existingEntity!;
            }
            entity.Id = Guid.Empty;
            var addedEntityEntry = await _dbSet.AddAsync(entity);
            _cache.Add(addedEntityEntry.Entity);
            return addedEntityEntry.Entity;
        }

        public virtual async Task<List<TEntity>> AddRange(List<TEntity> entities)
        {
            if (entities == null) return entities;

            await _dbSet.AddRangeAsync(entities);
            _cache.AddRange(entities);

            return entities;
        }

        public virtual async Task<TEntity> Update(TEntity entity)
        {
            if (entity == null) return entity;

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

        public virtual async Task<List<TEntity>> UpdateRange(List<TEntity> entities)
        {
            if (entities == null) return entities;

            var entitiesToUpdate = new List<TEntity>();

            foreach (var entity in entities)
            {
                if (Exists(entity, out TEntity? existingEntity))
                {
                    if (entity.Id == Guid.Empty && existingEntity.Id != Guid.Empty)
                    {
                        entity.Id = existingEntity.Id;
                    }

                    var tracked = _context.ChangeTracker.Entries<TEntity>()
                        .FirstOrDefault(e => e.Entity.Id == entity.Id);

                    if (tracked != null)
                    {
                        tracked.CurrentValues.SetValues(entity);
                    }
                    else
                    {
                        var local = _context.Set<TEntity>().Local
                            .FirstOrDefault(e => e.Id == entity.Id);

                        if (local != null)
                        {
                            _context.Entry(local).State = EntityState.Detached;
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
                _context.Attach(entity);
                _context.Entry(entity).State = EntityState.Modified;
            }

            _cache.UpdateRange(entitiesToUpdate);

            return entities;
        }

        public virtual async Task<TEntity> AddOrUpdate(TEntity entity)
        {
            if (entity == null)
                return entity;

            if (Exists(entity, out TEntity? existingEntity))
            {
                if (entity.Id == Guid.Empty && existingEntity != null && existingEntity.Id != Guid.Empty)
                    entity.Id = existingEntity.Id;
                return await Update(entity);
            }
            else
            {
                return await Add(entity);
            }
        }

        public virtual async Task<List<TEntity>> AddOrUpdateRange(List<TEntity> entities)
        {
            if (entities == null)
                return entities;

            var all = await _cache.GetAllAsync();
            var entitiesToAdd = new List<TEntity>();
            var entitiesToUpdate = new List<TEntity>();

            foreach (TEntity entity in entities)
            {
                if (Exists(entity, out TEntity? existingEntity))
                {
                    if (entity.Id == Guid.Empty && existingEntity.Id != Guid.Empty)
                        entity.Id = existingEntity.Id;
                    entitiesToUpdate.Add(entity);
                }
                else if (entitiesToAdd.Contains(entity))
                {
                    continue;
                }
                else
                {
                    entitiesToAdd.Add(entity);
                }
            }
            await AddRangeAsync(entitiesToAdd);
            await UpdateRange(entitiesToUpdate);

            return entitiesToAdd.Concat(entitiesToUpdate).ToList();
        }

        public virtual async Task AddRangeAsync(List<TEntity> entities)
        {
            if (entities == null || !entities.Any()) return;

            foreach (var entity in entities)
            {
                var tracked = _context.Set<TEntity>()
                                      .Local
                                      .FirstOrDefault(e => e.Id == entity.Id);
                if (tracked != null)
                {
                    _context.Entry(tracked).State = EntityState.Detached;
                }
            }

            await _dbSet.AddRangeAsync(entities);
            _cache.AddRange(entities);
        }

        public virtual async Task Delete(TEntity entity)
        {
            if (entity == null) return;

            TEntity? existingEntity = await GetById(entity.Id);
            if (existingEntity != null)
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
    }
}
