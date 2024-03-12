using System;
using System.ComponentModel.DataAnnotations;
using Api.Extension;
using Api.Properties;
using PhoneNumbers;

namespace Api.Data.DataAnnotations
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class MobilePhoneNumberAttribute : ValidationAttribute
    {
        private readonly string? _regionPropertyName;

        public MobilePhoneNumberAttribute(string? regionPropertyName = null)
        {
            _regionPropertyName = regionPropertyName;
        }

        protected sealed override ValidationResult IsValid(object value, ValidationContext context)
        {
            if (value == null)
                return ValidationResult.Success;
            if (!(value is string stringValue))
                throw new InvalidOperationException($"value ({value}) is not string");

            string? region = null;
            if (context != null && _regionPropertyName != null)
            {
                object instance = context.ObjectInstance;
                Type type = instance.GetType();
                var regionPropertyInfo = type.GetProperty(_regionPropertyName);
                if (regionPropertyInfo == null)
                    throw new InvalidOperationException($"regionProperty ({_regionPropertyName}) dose not exist");
                var regionObject = regionPropertyInfo.GetValue(instance);
                if (regionObject == null || !(regionObject is string regionValue))
                    throw new InvalidOperationException(
                        $"regionProperty ({_regionPropertyName}) value is not string({regionObject})");
                region = regionValue;
            }

            var number = stringValue.parseToPhoneNumber(region);
            if (number == null)
                return new ValidationResult(Resources.Phone_number_not_valid_);

            return IsValid(number, stringValue);
        }


        protected virtual ValidationResult IsValid(PhoneNumber number, string stringValue)
        {
            if (!PhoneNumberUtil.GetInstance().IsMobile(number))
                return new ValidationResult(Resources.Can_t_send_SMS_to_phone_number_);
            return ValidationResult.Success;
        }
    }
}