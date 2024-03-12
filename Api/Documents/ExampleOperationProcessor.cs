using System.Linq;
using Api.Data.DataAnnotations;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Api.Documents
{
    public class ExampleOperationProcessor : IOperationProcessor
    {
        public bool Process(OperationProcessorContext context)
        {
            foreach (var pair in context.Parameters)
            {
                if (pair.Key.CustomAttributes.Any(x => x.AttributeType == typeof(UuidAttribute)))
                {
                    if (pair.Value.Format == null)
                        pair.Value.Format = "uuid";
                    pair.Value.Example = "00000000-0000-0000-0000-000000000000";
                }
            }

            return true;
        }
    }
}