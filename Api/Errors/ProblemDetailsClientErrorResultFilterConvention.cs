using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Api.Errors
{
    public class ProblemDetailsClientErrorResultFilterConvention : IActionModelConvention
    {
        private readonly ProblemDetailsClientErrorResultFilterFactory _filterFactory = new ProblemDetailsClientErrorResultFilterFactory();

        public void Apply(ActionModel action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action.Filters.Add(_filterFactory);
        }
    }
}