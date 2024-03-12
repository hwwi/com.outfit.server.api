using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Api.Errors
{
    public class OutfitProblemDetailsFactory : ProblemDetailsFactory
    {
        private readonly ApiBehaviorOptions _apiBehaviorOptions;
        private readonly JsonOptions _jsonOptions;
        private readonly IStringLocalizer<OutfitProblemDetailsFactory> _localizer;

        public OutfitProblemDetailsFactory(IOptions<ApiBehaviorOptions> options, IOptions<JsonOptions> jsonOptions,
            IStringLocalizer<OutfitProblemDetailsFactory> localizer)
        {
            _apiBehaviorOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
            _jsonOptions = jsonOptions.Value ?? throw new ArgumentNullException(nameof(jsonOptions));
            _localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
        }

        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string title = null,
            string type = null,
            string detail = null,
            string instance = null)
        {
            statusCode ??= 500;

            var problemDetails = new ProblemDetails {
                Status = statusCode,
                Title = title,
                Type = type,
                Detail = detail,
                Instance = instance,
            };

            ApplyProblemDetailsDefaults(httpContext, problemDetails, statusCode.Value);

            return problemDetails;
        }

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelState,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
        {
            if (modelState == null)
            {
                throw new ArgumentNullException(nameof(modelState));
            }

            statusCode ??= 400;

            // https://github.com/dotnet/corefx/issues/38840
            // options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase is only works for deserialization 
            var modelStateDictionary = new Dictionary<string, string[]>();
            foreach (var pair in modelState)
            {
                var errors = pair.Value.Errors;
                if (errors == null || errors.Count <= 0)
                    continue;
                var key = _jsonOptions.JsonSerializerOptions.PropertyNamingPolicy
                    .ConvertName(pair.Key) ?? pair.Key;
                modelStateDictionary.Add(key, errors.Select(x => x.ErrorMessage).ToArray());
            }


            var problemDetails = new ValidationProblemDetails(modelStateDictionary) {
                Status = statusCode, Type = type, Detail = detail, Instance = instance,
            };

            if (title != null)
            {
                // For validation problem details, don't overwrite the default title with null.
                problemDetails.Title = title;
            }

            ApplyProblemDetailsDefaults(httpContext, problemDetails, statusCode.Value);

            return problemDetails;
        }

        private void ApplyProblemDetailsDefaults(HttpContext httpContext, ProblemDetails problemDetails, int statusCode)
        {
            problemDetails.Status ??= statusCode;

            if (_apiBehaviorOptions.ClientErrorMapping.TryGetValue(statusCode, out var clientErrorData))
            {
                problemDetails.Title ??= clientErrorData.Title;
                problemDetails.Type ??= clientErrorData.Link;
            }

            var traceId = Activity.Current?.Id ?? httpContext?.TraceIdentifier;
            if (traceId != null)
            {
                problemDetails.Extensions["traceId"] = traceId;
            }

            problemDetails.Detail ??= _localizer[problemDetails.Title];
        }
    }
}