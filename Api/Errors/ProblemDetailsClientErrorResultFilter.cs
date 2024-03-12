using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Api.Errors
{
    public class ProblemDetailsClientErrorResultFilter : IAlwaysRunResultFilter, IOrderedFilter
    {
        internal const int FilterOrder = int.MaxValue - 10;
        public int Order { get; } = FilterOrder;
        private readonly ProblemDetailsFactory _problemDetailsFactory;
        private readonly ILogger<ProblemDetailsClientErrorResultFilter> _logger;

        public ProblemDetailsClientErrorResultFilter(ProblemDetailsFactory problemDetailsFactory,
            ILogger<ProblemDetailsClientErrorResultFilter> logger)
        {
            _problemDetailsFactory =
                problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void OnResultExecuted(ResultExecutedContext context)
        {
        }

        public void OnResultExecuting(ResultExecutingContext context)
        {
            if (context.Result is IClientErrorActionResult clientError)
            {
                if (clientError.StatusCode < 400)
                {
                    return;
                }

                //TODO 궂이 필요할까?
                ProblemDetails problemDetails =
                    _problemDetailsFactory.CreateProblemDetails(context.HttpContext, clientError.StatusCode);
                context.Result =
                    new ObjectResult(problemDetails) {
                        StatusCode = clientError.StatusCode,
                        ContentTypes = {"application/problem+json", "application/problem+xml",},
                    };
            }
            else if (context.Result is ObjectResult objectResult)
            {
                if (objectResult.StatusCode < 400)
                {
                    return;
                }

                if (objectResult.Value is String stringValue)
                {
                    ProblemDetails problemDetails =
                        _problemDetailsFactory.CreateProblemDetails(context.HttpContext, objectResult.StatusCode,
                            stringValue);
                    objectResult.Value = problemDetails;
                }
            }
        }
    }
}