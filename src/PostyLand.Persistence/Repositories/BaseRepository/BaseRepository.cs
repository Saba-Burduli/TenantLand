using Microsoft.EntityFrameworkCore;
using PostyLand.Persistence.Repositories.Interfaces;

namespace PostyLand.Persistence.Repositories.BaseRepository;

public class BaseRepository<TEntity>(DbContext dbContext) : IBaseRepository<TEntity>
    where TEntity : class
{
    protected readonly DbContext DbContext = dbContext;
    protected readonly DbSet<TEntity> DbSet = dbContext.Set<TEntity>();

    public virtual async Task<TEntity?> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await DbSet.ToListAsync();
    }

    public virtual async Task AddAsync(TEntity entity)
    {
        await DbSet.AddAsync(entity);
    }

    public virtual void Update(TEntity entity)
    {
        DbSet.Update(entity);
    }

    public virtual void Delete(TEntity entity)
    {
        DbSet.Remove(entity);
    }

    public virtual async Task SaveChangesAsync()
    {
        await DbContext.SaveChangesAsync();
    }
}
