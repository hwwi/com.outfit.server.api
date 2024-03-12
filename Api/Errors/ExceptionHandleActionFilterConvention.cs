using System;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Api.Errors
{
    public class ExceptionHandleActionFilterConvention : IActionModelConvention
    {
        private readonly ExceptionHandleActionFilterFactory _filterFactory = new ExceptionHandleActionFilterFactory();

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