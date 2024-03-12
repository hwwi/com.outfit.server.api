using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Data;
using Api.Data.Args;
using Api.Data.Dto;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories
{
    public abstract class AbstractEntityRepository<T> : AbstractRepository<T> where T : class, IEntity
    {
        protected AbstractEntityRepository(OutfitDbContext context) : base(context) { }


        public async Task<T?> FindOneByIdAsync(long id)
        {
            return await FindOneAsync(x => x.Id == id);
        }
    }
}