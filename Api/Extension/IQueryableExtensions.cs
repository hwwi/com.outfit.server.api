using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Api.Data;
using Api.Data.Args;
using Api.Data.Dto;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace Api.Extension
{
    public static class IQueryableExtensions
    {
        public static async Task<Connection<TResult>> ToConnectionAsync<TSource, TResult>(
            this IQueryable<TSource> queryable,
            ConnectionArgs args,
            IMapper mapper,
            object? parameter = null
        )
            where TSource : IIdentifiability
            where TResult : IIdentifiability
        {
            if (args.Cursor != null)
            {
                queryable = args.Direction == Direction.After
                    ? queryable.Where(x => x.Id >= args.Cursor)
                    : queryable.Where(x => x.Id <= args.Cursor);
            }

            queryable = args.Direction == Direction.After
                ? queryable.OrderBy(x => x.Id)
                : queryable.OrderByDescending(x => x.Id);

            return await queryable
                .TakeConnectionAsync<TSource, TResult>(args, mapper, parameter);
        }

        public static async Task<Connection<TResult>> TakeConnectionAsync<TSource, TResult>(
            this IQueryable<TSource> queryable,
            ConnectionArgs args,
            IMapper mapper,
            object? parameter = null
        )
            where TResult : IIdentifiability
        {
            int take = args.Limit + (args.Cursor == null ? 1 : 2);

            List<TResult> results = await queryable
                    .Take(take)
                    .ProjectTo<TResult>(mapper.ConfigurationProvider, parameter)
                    .ToListAsync()
                ;

            bool hasCursor = args.Cursor != null
                             && results.FirstOrDefault()?.Id == args.Cursor;

            if (results.Count <= (hasCursor ? 1 : 0))
            {
                return new Connection<TResult> {
                    Edges = new List<TResult>(),
                    PageInfo = new PageInfo {StartCursor = null, EndCursor = null, HasMorePage = false}
                };
            }

            bool hasMorePage = results.Count == take - (args.Cursor != null && !hasCursor ? 1 : 0);
            int rangeIndex = hasCursor ? 1 : 0;
            int rangeCount = results.Count - (hasCursor ? 1 : 0) - (hasMorePage ? 1 : 0);

            List<TResult> edges = results.GetRange(rangeIndex, rangeCount);

            if ((int)args.Direction != (int)args.SortOrder)
                edges.Reverse();

            return new Connection<TResult> {
                Edges = edges,
                PageInfo = new PageInfo {
                    StartCursor = edges.FirstOrDefault()?.Id,
                    EndCursor = edges.LastOrDefault()?.Id,
                    HasMorePage = hasMorePage
                }
            };
        }
    }
}