using System;
using System.ComponentModel.DataAnnotations;

namespace Api.Data.DataAnnotations
{
    public abstract class AbstractValidationAttribute : ValidationAttribute
    {
        protected sealed override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            if (validationContext == null)
                throw new ArgumentNullException(nameof(validationContext));

            var errorMsg = getValidateErrorMsg(validationContext, validationContext.MemberName, value);
            return errorMsg == null
                ? ValidationResult.Success
                : new ValidationResult(errorMsg,
                    validationContext.MemberName != null ? new[] {validationContext.MemberName} : null
                );
        }


        protected abstract string? getValidateErrorMsg(
            ValidationContext context,
            string? memberName,
            object? value
        );
    }
}