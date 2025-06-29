using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Interfaces;
using System.Linq.Expressions;

namespace MolenApplicatie.Server.Services.Database
{
    public abstract class DBDefaultService<TEntity> where TEntity : class, DefaultModel, IEquatable<TEntity>
    {
        private readonly MolenDbContext _context;
        private readonly DbSet<TEntity> _dbSet;
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

        public virtual async Task<TEntity?> GetById(int id)
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

            var addedEntityEntry = await _dbSet.AddAsync(entity);
            _cache.Add(addedEntityEntry.Entity);
            return addedEntityEntry.Entity;
        }

        public virtual async Task<List<TEntity>> AddRange(List<TEntity> entities)
        {
            if (entities == null) return entities;

            await _dbSet.AddRangeAsync(entities);

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

            _dbSet.UpdateRange(entities);
            await Task.CompletedTask;
            return entities;
        }

        public virtual async Task<TEntity> AddOrUpdate(TEntity entity)
        {
            if (entity == null)
                return entity;

            if (Exists(entity, out TEntity? existingEntity))
            {
                if (entity.Id == 0 && existingEntity != null && existingEntity.Id != 0)
                    entity.Id = existingEntity.Id;

                await Update(entity);
                return entity;
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
                var existingEntity = all.FirstOrDefault(e => e.Equals(entity));
                if (existingEntity != null)
                {
                    if (entity.Id == 0 && existingEntity.Id != 0)
                        entity.Id = existingEntity.Id;

                    _context.Entry(existingEntity).CurrentValues.SetValues(entity);
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

            return entities;
        }

        public virtual async Task AddRangeAsync(List<TEntity> entities)
        {
            if (entities == null) return;

            foreach (var entity in entities)
            {
                entity.Id = 0;
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
            Console.WriteLine($"Checking existence in cache: {exists} - {existing?.Id}");
            if (exists) return true;
            existing = _context.ChangeTracker
                .Entries<TEntity>()
                .FirstOrDefault(e =>
                    (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Unchanged) &&
                    predicate.Compile().Invoke(e.Entity)
                )?.Entity;
            if (existing != null)
            {
                _cache.Add(existing);
                return true;
            }
            existing = _dbSet.Local.SingleOrDefault(predicate.Compile());
            if (existing != null)
            {
                _cache.Add(existing);
                return true;
            }
            existing = _dbSet.SingleOrDefault(predicate);
            if (existing != null)
            {
                _cache.Add(existing);
                return true;
            }
            return false;
        }
    }
}
