using System;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Errors
{
    public class ProblemDetailsClientErrorResultFilterFactory : IFilterFactory, IOrderedFilter
    {
        public bool IsReusable => true;
        public int Order => ProblemDetailsClientErrorResultFilter.FilterOrder;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return ActivatorUtilities.CreateInstance<ProblemDetailsClientErrorResultFilter>(serviceProvider);
        }
    }
}