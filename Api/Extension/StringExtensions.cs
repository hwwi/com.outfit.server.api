using System;
using Castle.Core.Internal;
using PhoneNumbers;

namespace Api.Extension
{
    public static class StringExtensions
    {
        public static PhoneNumber? parseToPhoneNumber(
            this string unParsed,
            string? region = null
        )
        {
            PhoneNumberUtil phoneNumberUtil = PhoneNumberUtil.GetInstance();
            PhoneNumber number;
            try
            {
                number = phoneNumberUtil.Parse(unParsed, region);
            }
            catch (Exception e)
            {
                return null;
            }

            return phoneNumberUtil.IsValidNumber(number) ? number : null;
        }

        public static string format(
            this string unParsed,
            PhoneNumberFormat numberFormat,
            string? region = null
        )
        {
            var number = unParsed.parseToPhoneNumber(region)
                         ?? throw new InvalidOperationException($"unParsed({unParsed}) is not validate");
            return PhoneNumberUtil.GetInstance()
                .Format(number, numberFormat);
        }

        public static string Truncate(this string value, int maxLength, string? replace = null)
        {
            if (value.IsNullOrEmpty() || value.Length <= maxLength)
                return value;
            string substring = value.Substring(0, maxLength);
            if (replace == null)
                return substring;
            return substring + replace;
        }
    }
}