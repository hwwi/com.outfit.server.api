using System;
using System.ComponentModel.DataAnnotations;
using Api.Properties;

namespace Api.Data.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class RequiredIfNotNullAttribute : ValidationAttribute
    {
        private string _ifPropertyName;

        public RequiredIfNotNullAttribute(string ifPropertyName)
        {
            _ifPropertyName = ifPropertyName ?? throw new ArgumentNullException(nameof(ifPropertyName));
            ErrorMessage = Resources.The__0__field_is_required_;
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            object instance = context.ObjectInstance;
            Type type = instance.GetType();
            var ifPropertyInfo = type.GetProperty(_ifPropertyName);

            if (ifPropertyInfo == null)
                throw new InvalidOperationException($"ifProperty ({_ifPropertyName}) dose not exist");

            return ifPropertyInfo.GetValue(instance) == null || value != null
                ? ValidationResult.Success
                : new ValidationResult(FormatErrorMessage(context.MemberName));
        }
    }
}