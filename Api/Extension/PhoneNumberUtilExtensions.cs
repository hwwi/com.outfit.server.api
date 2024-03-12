using PhoneNumbers;

namespace Api.Extension
{
    public static class PhoneNumberUtilExtensions
    {
        public static bool IsMobile(
            this PhoneNumberUtil numberUtil,
            PhoneNumber number,
            string? region = null)
        {
            return numberUtil.GetNumberType(number) switch {
                var x
                when x is PhoneNumberType.MOBILE
                     || x is PhoneNumberType.FIXED_LINE_OR_MOBILE => true,
                _ => false
            };
        }
    }
}