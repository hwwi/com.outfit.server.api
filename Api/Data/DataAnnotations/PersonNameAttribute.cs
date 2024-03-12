using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Api.Properties;

namespace Api.Data.DataAnnotations
{
    public class PersonNameAttribute : AbstractValidationAttribute
    {
        private const int MinLength = 5;
        private const int MaxLength = 20;

        protected override string? getValidateErrorMsg(ValidationContext context, string? memberName, object? value)
        {
            if (!(value is String name) || MinLength > name.Length || name.Length > MaxLength)
                return string.Format(Resources.Must_be_between__0__and__1__characters_long_, MinLength, MaxLength);


            if (!Regex.Match(name, "^(?=.*?[a-z])(?=.*?[_])[^.]+$").Success)
                return Resources.Must_use_at_least_one_alphabet_and_underscore_;

            if (Regex.Match(name, "_{2,}").Success)
                return Resources.Continuous_underscore_is_not_allowed_;

            if (!Regex.Match(name, "^[a-z0-9_]+$").Success)
                return Resources.Only_lowercase_alphanumeric_or_underscore_characters_a_z_0_9____allowed_;

            return null;
        }
    }
}