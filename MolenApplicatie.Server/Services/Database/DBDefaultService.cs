using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Interfaces;
using System.Linq.Expressions;

namespace MolenApplicatie.Server.Services.Database
{
    public abstract class DBDefaultService<TEntity> where TEntity : class, DefaultModel
    {
        private readonly MolenDbContext _context;
        private readonly DbSet<TEntity> _dbSet;

        public DBDefaultService(MolenDbContext context)
        {
            _context = context;
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

            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task<List<TEntity>> AddRange(List<TEntity> entities)
        {
            if (entities == null) return entities;

            var result = new List<TEntity>();
            foreach (TEntity entity in entities)
            {
                var added = await Add(entity);
                result.Add(added);
            }

            return result;
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
            await Task.CompletedTask;
            return entity;
        }

        public virtual async Task<List<TEntity>> UpdateRange(List<TEntity> entities)
        {
            if (entities == null) return entities;

            foreach (TEntity entity in entities)
            {
                await Update(entity);
            }
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

            foreach (TEntity entity in entities.ToList())
            {
                await AddOrUpdate(entity);
            }

            return entities;
        }

        public virtual async Task Delete(TEntity entity)
        {
            if (entity == null) return;

            TEntity? existingEntity = await GetById(entity.Id);
            if (existingEntity != null)
            {
                _dbSet.Remove(existingEntity);
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
            existing = _context.ChangeTracker
                .Entries<TEntity>()
                .FirstOrDefault(e =>
                    (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Unchanged) &&
                    predicate.Compile().Invoke(e.Entity)
                )?.Entity;
            if (existing != null) return true;
            existing = _dbSet.Local.SingleOrDefault(predicate.Compile());
            if (existing != null) return true;
            existing = _dbSet.SingleOrDefault(predicate);
            return existing != null;
        }
    }
}
