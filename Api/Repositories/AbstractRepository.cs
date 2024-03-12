using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Api.Repositories
{
    public abstract class AbstractRepository<T>
        where T : class
    {
        protected OutfitDbContext Context { get; }

        protected DbSet<T> Set { get; }

        protected AbstractRepository(OutfitDbContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            Set = Context.Set<T>();
        }

        public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await Set.Where(predicate)
                .ToListAsync();
        }

        public async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate)
        {
            return await Set.Where(predicate)
                .SingleOrDefaultAsync<T?>();
        }

        public bool Any(Expression<Func<T, bool>> predicate)
        {
            return Set.Any(predicate);
        }
        [Obsolete]
        public async Task<int> UpdateAsync(T t)
        {
            Set.Update(t);
            return await Context.SaveChangesAsync();
        }
    }
}