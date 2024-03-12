using System;
using System.ComponentModel.DataAnnotations;
using Api.Properties;
using PhoneNumbers;

namespace Api.Data.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class MobileE164PhoneNumberAttribute : MobilePhoneNumberAttribute
    {
        public MobileE164PhoneNumberAttribute() : base(null)
        {
        }

        protected override ValidationResult IsValid(PhoneNumber number, string stringValue)
        {
            var result = base.IsValid(number, stringValue);
            if (result == ValidationResult.Success)
                return result;
            var e164Number = PhoneNumberUtil.GetInstance().Format(number, PhoneNumberFormat.E164);
            if (stringValue != e164Number)
                return new ValidationResult(string.Format(Resources.The__0__is_not_in_E164_format_, stringValue));

            return ValidationResult.Success;
        }
    }
}