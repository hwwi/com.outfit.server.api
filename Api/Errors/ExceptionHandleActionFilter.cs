using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Api.Errors
{
    public class ExceptionHandleActionFilter : IActionFilter, IOrderedFilter
    {
        internal const int FilterOrder = int.MaxValue - 10;
        public int Order { get; } = FilterOrder;
        private readonly ILogger<ExceptionHandleActionFilter> _logger;
        private readonly ProblemDetailsFactory _problemDetailsFactory;
        private readonly IWebHostEnvironment _environment;

        public ExceptionHandleActionFilter(
            ILogger<ExceptionHandleActionFilter> logger,
            ProblemDetailsFactory problemDetailsFactory,
            IWebHostEnvironment environment
        )
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _problemDetailsFactory =
                problemDetailsFactory ?? throw new ArgumentNullException(nameof(problemDetailsFactory));
            _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            if (context.Exception != null)
            {
                int? statusCode = null;
                string? title = null;
                string? detail = null;

                ProblemDetails problemDetails;
                if (context.Exception is ProblemDetailsException exception)
                {
                    statusCode = exception.StatusCode;
                    title = exception.Title;
                    detail = exception.Detail;
                }

                if (statusCode == null)
                    statusCode = 500;

                MediaTypeCollection mediaTypeCollection;
                if (context.Exception is ValidationProblemDetailsException validationProblemDetailsException)
                {
                    problemDetails =
                        _problemDetailsFactory.CreateValidationProblemDetails(context.HttpContext,
                            validationProblemDetailsException.ModelState,
                            statusCode: statusCode,
                            title: title,
                            detail: detail
                        );
                    mediaTypeCollection = new MediaTypeCollection {
                        "application/validation.problem+json", "application/validation.problem+xml"
                    };
                }
                else
                {
                    problemDetails =
                        _problemDetailsFactory.CreateProblemDetails(context.HttpContext,
                            statusCode: statusCode,
                            title: title,
                            detail: detail
                        );
                    mediaTypeCollection = new MediaTypeCollection {
                        "application/problem+json", "application/problem+xml"
                    };
                }

                _logger.LogError(context.Exception,
                    $"Action executed with exception. statusCode: {statusCode}, title: {title}, detail: {detail}"
                );


                if (!_environment.IsProduction())
                {
                    problemDetails.Extensions.Add("exception",
                        new {
                            typeName = context.Exception.GetType().Name,
                            message = context.Exception.Message,
                            stackTrace = context.Exception.StackTrace,
                        });
                }

                context.Result =
                    new ObjectResult(problemDetails) {StatusCode = statusCode, ContentTypes = mediaTypeCollection};
                context.ExceptionHandled = true;
            }
        }
    }
}