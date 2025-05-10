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
            if (Exists(entity, out TEntity? existingEntity))
            {
                return existingEntity!;
            }
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task<List<TEntity>> AddRange(List<TEntity> entities)
        {
            foreach (TEntity entity in entities)
            {
                await Add(entity);
            }
            return entities;
        }

        public virtual TEntity Update(TEntity entity)
        {
            _dbSet.Update(entity);
            return entity;
        }

        public virtual List<TEntity> UpdateRange(List<TEntity> entities)
        {
            foreach (TEntity entity in entities)
            {
                Update(entity);
            }
            return entities;
        }

        public virtual async Task<TEntity> AddOrUpdate(TEntity entity)
        {
            if (Exists(entity, out TEntity? existingEntity))
            {
                Update(existingEntity!);
                return existingEntity!;
            }
            else
            {
                return await Add(entity);
            }
        }

        public virtual async Task<List<TEntity>> AddOrUpdateRange(List<TEntity> entities)
        {
            foreach (TEntity entity in entities)
            {
                await AddOrUpdate(entity);
            }
            return entities;
        }

        public virtual async Task Delete(TEntity entity)
        {
            TEntity? existingEntity = await GetById(entity.Id);
            if (existingEntity != null)
            {
                _dbSet.Remove(existingEntity);
            }
        }

        public virtual async Task DeleteRange(List<TEntity> entities)
        {
            foreach (var entity in entities)
            {
                await Delete(entity);
            }
        }

        public abstract bool Exists(TEntity entity, out TEntity? existing);

        public virtual bool Exists(Expression<Func<TEntity, bool>> predicate, out TEntity? existing)
        {
            existing = _context.ChangeTracker.Entries<TEntity>()
                .Select(e => e.Entity)
                .FirstOrDefault(predicate.Compile());

            if (existing == null)
                existing = _dbSet.Local.SingleOrDefault(predicate.Compile())!;

            if (existing == null)
                existing = _dbSet.SingleOrDefault(predicate)!;

            return existing != null;
        }
    }
}
