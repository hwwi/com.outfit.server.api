using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Errors
{
    public class ValidationProblemDetailsException : ProblemDetailsException
    {
        public ValidationProblemDetailsException(ModelStateDictionary modelState)
        {
            ModelState = modelState;
        }

        public ModelStateDictionary ModelState { get; set; }
    }
}