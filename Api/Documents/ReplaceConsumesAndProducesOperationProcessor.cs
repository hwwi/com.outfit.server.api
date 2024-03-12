using System;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Api.Documents
{
    public class ReplaceConsumesAndProducesOperationProcessor : IOperationProcessor
    {
        private readonly string _contentType;

        public ReplaceConsumesAndProducesOperationProcessor(string contentType)
        {
            _contentType = contentType ?? throw new ArgumentNullException(nameof(contentType));
        }


        public bool Process(OperationProcessorContext context)
        {
            context.OperationDescription.Operation.Consumes.Clear();
            context.OperationDescription.Operation.Consumes.Add(_contentType);
            context.OperationDescription.Operation.Produces.Clear();
            context.OperationDescription.Operation.Produces.Add(_contentType);

            return true;
        }
    }
}