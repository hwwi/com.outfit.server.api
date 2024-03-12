using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;

namespace Api.Configuration.ModelBinders
{
    public class FromFormJsonStringAttribute : ModelBinderAttribute
    {
        public FromFormJsonStringAttribute() : base(typeof(JsonModelBinder)) { }
    }

    public class JsonModelBinder : IModelBinder
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public JsonModelBinder(IOptions<JsonOptions> jsonOptions)
        {
            _jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions
                                     ?? throw new ArgumentNullException(nameof(jsonOptions));
        }

        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
            if (valueProviderResult != ValueProviderResult.None)
            {
                bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

                var valueAsString = valueProviderResult.FirstValue;

                var result = JsonSerializer.Deserialize(
                    valueAsString,
                    bindingContext.ModelType,
                    _jsonSerializerOptions
                );
                if (result != null)
                {
                    bindingContext.Result = ModelBindingResult.Success(result);
                    return Task.CompletedTask;
                }
            }

            return Task.CompletedTask;
        }
    }
}