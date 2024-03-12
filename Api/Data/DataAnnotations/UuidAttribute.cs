using System;
using System.ComponentModel.DataAnnotations;
using Api.Properties;

namespace Api.Data.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class UuidAttribute : ValidationAttribute
    {
        public UuidAttribute() : base(Resources.The__0__field_is_not_a_valid_uuid_)
        {
        }

        public override bool IsValid(object value)
        {
            return value == null || value is string str && Guid.TryParse(str, out var guid);
        }
    }
}