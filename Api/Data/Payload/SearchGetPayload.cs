using System.Collections.Generic;
using Api.Data.Dto;

namespace Api.Data.Payload
{
    public class SearchGetPayload
    {
        public List<PersonDto>? Persons { get; set; }
        public List<SearchedHashTag>? HashTags { get; set; }
        public List<SearchedItemTag>? ItemTags { get; set; }
    }
}