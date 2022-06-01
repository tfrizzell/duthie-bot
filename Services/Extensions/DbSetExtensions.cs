using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Duthie.Services.Extensions;

public static class DbSetExtensions
{
    public static Task<EntityEntry<TEntity>> RemoveAsync<TEntity>(this DbSet<TEntity> dbSet, TEntity entity) where TEntity : class =>
        Task.FromResult<EntityEntry<TEntity>>(dbSet.Remove(entity));

    public static Task RemoveRangeAsync<TEntity>(this DbSet<TEntity> dbSet, IEnumerable<TEntity> entities) where TEntity : class
    {
        dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public static Task RemoveRangeAsync<TEntity>(this DbSet<TEntity> dbSet, params TEntity[] entities) where TEntity : class
    {
        dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public static Task<EntityEntry<TEntity>> UpdateAsync<TEntity>(this DbSet<TEntity> dbSet, TEntity entity) where TEntity : class =>
        Task.FromResult<EntityEntry<TEntity>>(dbSet.Update(entity));
}