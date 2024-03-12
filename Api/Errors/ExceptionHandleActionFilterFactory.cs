using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Errors
{
    public class ExceptionHandleActionFilterFactory : IFilterFactory, IOrderedFilter
    {
        public bool IsReusable => true;
        public int Order => ExceptionHandleActionFilter.FilterOrder;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<ExceptionHandleActionFilter>(serviceProvider);
        }
    }
}