using System.Text.RegularExpressions;

namespace Api.Utils
{
    public static class Regexs
    {
        public const char StartCharPersonTag = '@';
        public const char StartCharHashTag = '#';
        public const char StartCharItemTag = '&';

        public const string GroupPersonName = "name";
        public static readonly Regex PersonTag = new Regex("@(?<name>[a-zA-Z0-9_]+)");

        public static readonly string GroupHashName = "name";
        public static readonly Regex HashTag = new Regex(@"(?<=\s|^)#(?<name>\w*[A-Za-z_]+\w*)");


        public const string GroupItemBrandCode = "brandCode";
        public const string GroupItemProductCode = "productCode";

        public static readonly Regex Alphanumeric = new Regex("^[a-zA-Z0-9_]+$");
        public static readonly Regex UpperAlphanumeric = new Regex("^[A-Z0-9_]+$");

        public static readonly Regex ItemTag =
            new Regex("&(?<brandCode>[a-zA-Z0-9_]+)/(?<productCode>[a-zA-Z0-9_]+)");
    }
}