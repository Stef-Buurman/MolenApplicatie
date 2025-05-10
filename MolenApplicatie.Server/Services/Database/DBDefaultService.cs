using Microsoft.EntityFrameworkCore;
using MolenApplicatie.Server.Data;
using MolenApplicatie.Server.Interfaces;

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

        public virtual async Task<TEntity?> GetById(object id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<TEntity> Add(TEntity entity)
        {
            if (Exists(entity, out var existingEntity))
            {
                return existingEntity!;
            }
            await _dbSet.AddAsync(entity);
            return entity;
        }

        public virtual async Task<List<TEntity>> AddRange(List<TEntity> entities)
        {
            foreach (var entity in entities)
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
            foreach (var entity in entities)
            {
                Update(entity);
            }
            return entities;
        }

        public virtual async Task<TEntity> AddOrUpdate(TEntity entity)
        {
            if (Exists(entity, out var existingEntity))
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
            foreach (var entity in entities)
            {
                await AddOrUpdate(entity);
            }
            return entities;
        }

        public virtual async Task Delete(TEntity entity)
        {
            var existingEntity = await GetById(entity.Id);
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
    }
}
