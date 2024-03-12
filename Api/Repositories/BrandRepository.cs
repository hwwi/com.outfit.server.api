using System;
using System.Collections.Generic;
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
    public class BrandRepository : AbstractEntityRepository<Brand>
    {
        private readonly IMapper _mapper;

        public BrandRepository(OutfitDbContext context, IMapper mapper) : base(context)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
    }
}