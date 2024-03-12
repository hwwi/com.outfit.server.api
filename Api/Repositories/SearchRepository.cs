using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Api.Data;
using Api.Data.Dto;
using Api.Data.Payload;
using Api.Utils;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Castle.Core.Internal;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories
{
    public class SearchRepository
    {
        private readonly PersonRepository _personRepository;
        private readonly BrandRepository _brandRepository;
        private readonly ProductRepository _productRepository;
        private readonly ShotRepository _shotRepository;
        private readonly OutfitDbContext _dbContext;
        private readonly IMapper _mapper;

        public SearchRepository(
            PersonRepository personRepository,
            BrandRepository brandRepository,
            ProductRepository productRepository,
            ShotRepository shotRepository,
            OutfitDbContext dbContext,
            IMapper mapper
        )
        {
            _personRepository = personRepository ?? throw new ArgumentNullException(nameof(personRepository));
            _brandRepository = brandRepository ?? throw new ArgumentNullException(nameof(brandRepository));
            _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
            _shotRepository = shotRepository ?? throw new ArgumentNullException(nameof(shotRepository));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        public async Task<SearchGetPayload> FindSearch(string query, int take = 10)
        {
            var payload = new SearchGetPayload();

            bool needSearchingPersonTag = false;
            bool needSearchingHashTag = false;
            bool needSearchingItemTag = false;

            if (query.Length > 0)
            {
                switch (query[0])
                {
                    case Regexs.StartCharPersonTag:
                        needSearchingPersonTag = true;
                        break;
                    case Regexs.StartCharHashTag:
                        needSearchingHashTag = true;
                        break;
                    case Regexs.StartCharItemTag:
                        needSearchingItemTag = true;
                        break;
                }

                if (needSearchingPersonTag || needSearchingHashTag || needSearchingItemTag)
                    query = query.Substring(1, query.Length - 1);
                else
                    needSearchingPersonTag = needSearchingHashTag = needSearchingItemTag = true;
            }


            payload.Persons = needSearchingPersonTag
                ? await _dbContext.Persons
                    .Where(x => EF.Functions.Like(x.Name, query + "%"))
                    .Take(take)
                    .ProjectTo<PersonDto>(_mapper.ConfigurationProvider)
                    .ToListAsync()
                : null;

            payload.HashTags = needSearchingHashTag
                ? await _dbContext.HashTags
                    .Where(x => EF.Functions.Like(x.Tag, query + "%"))
                    .Take(take)
                    .ProjectTo<SearchedHashTag>(_mapper.ConfigurationProvider)
                    .ToListAsync()
                : null;

            if (needSearchingItemTag)
            {
                string brandCode;
                string? productCode;
                if (query.Contains('/', StringComparison.InvariantCulture))
                {
                    var split = query.Split('/', 2);
                    brandCode = split[0];
                    productCode = split[1] + "%";
                }
                else
                {
                    brandCode = query + "%";
                    productCode = null;
                }

                payload.ItemTags =
                    productCode.IsNullOrEmpty()
                        ? await _dbContext.Brands
                            .Where(x => EF.Functions.Like(x.Code, brandCode))
                            .Take(take)
                            .Select(x => new SearchedItemTag {BrandCode = x.Code})
                            .ToListAsync()
                        : _dbContext.Products
                            .Include(x => x.Brand)
                            .Where(x => x.Brand.Code == brandCode && EF.Functions.Like(x.Code, productCode))
                            .Take(take)
                            .Select(x => new {BrandCode = x.Brand.Code, ProductCode = x.Code})
                            .ToList()
                            .GroupBy(x => x.BrandCode)
                            .Select(x => new SearchedItemTag {
                                BrandCode = x.Key, ProductCodes = x.Select(y => y.ProductCode).ToList()
                            })
                            .ToList()
                    ;
            }

            return payload;
        }
    }
}