using System;

namespace Api.Errors
{
    public class ProblemDetailsException : Exception
    {
        public int? StatusCode { get; set; }

        public string? Title { get; set; }

        public string? Detail { get; set; }
    }
}