using System.ComponentModel.DataAnnotations;
using Api.Properties;
using Api.Utils;

namespace Api.Data.DataAnnotations
{
    public class ProductCodeAttribute : AbstractValidationAttribute
    {
        private const int MinLength = 2;
        private const int MaxLength = 15;

        protected override string? getValidateErrorMsg(ValidationContext context, string? memberName, object? value)
        {
            if (!(value is string code))
                return Resources.Must_be_string_;

            if (code.Length == 0)
                return null;

            if (MinLength > code.Length || code.Length > MaxLength)
                return string.Format(Resources.Must_be_between__0__and__1__characters_long_, MinLength, MaxLength);


            if (!Regexs.UpperAlphanumeric.IsMatch(code))
                return string.Format(Resources.The__0__field_must_be_upper_alphanumeric_, memberName);

            return null;
        }
    }
}