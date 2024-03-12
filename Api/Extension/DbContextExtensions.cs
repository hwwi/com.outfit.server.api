using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Api.Extension
{
    public static class DbContextExtensions
    {
        public static async Task<EntityEntry<TEntity>> AddAndSaveAsync<TEntity>(this DbContext context, TEntity entity)
            where TEntity : class
        {
            EntityEntry<TEntity> valueTask = await context.Set<TEntity>().AddAsync(entity);

            await context.SaveChangesAsync();
            return valueTask;
        }
        public static async Task<EntityEntry<TEntity>> UpdateAndSaveAsync<TEntity>(this DbContext context, TEntity entity)
            where TEntity : class
        {
            EntityEntry<TEntity> valueTask = context.Set<TEntity>().Update(entity);
            await context.SaveChangesAsync();
            return valueTask;
        }
    }
}