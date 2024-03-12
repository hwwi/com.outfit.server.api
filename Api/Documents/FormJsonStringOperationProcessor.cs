using System.Linq;
using Api.Configuration.ModelBinders;
using NSwag;
using NSwag.Generation.Processors;
using NSwag.Generation.Processors.Contexts;

namespace Api.Documents
{
    // OpenApi can't use an object type for formData parameters. (https://github.com/swagger-api/swagger-ui/issues/1803)
    public class FormJsonStringOperationProcessor : IOperationProcessor
    {
        public bool Process(OperationProcessorContext context)
        {
            foreach (var pair in context.Parameters)
            {
                if (pair.Key.CustomAttributes.Any(x => x.AttributeType == typeof(FromFormJsonStringAttribute)))
                {
                    pair.Value.Kind = OpenApiParameterKind.FormData;
                    pair.Value.IsRequired = true;
                    pair.Value.IsNullableRaw = false;
                }
            }

            return true;
        }
    }
}