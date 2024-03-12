using System.Collections.Generic;

namespace Api.Data.Payload
{
    public class SearchedItemTag
    {
        public string BrandCode { get; set; }
        public List<string> ProductCodes { get; set; }
    }
}