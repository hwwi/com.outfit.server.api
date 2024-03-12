using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Api.Data;
using Api.Data.Dto;
using Api.Data.Models;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories
{
    public class ProductRepository : AbstractEntityRepository<Product>
    {
        private readonly IMapper _mapper;

        public ProductRepository(OutfitDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
    }
}