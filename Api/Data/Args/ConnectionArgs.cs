using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Api.Data.Args
{
    public class ConnectionArgs
    {
        [FromQuery(Name = "cursor")]
        public long? Cursor { get; set; }

        [FromQuery(Name = "direction"), BindRequired]
        public Direction Direction { get; set; }

        [FromQuery(Name = "sortOrder"), BindRequired]
        public SortOrder SortOrder { get; set; }

        [FromQuery(Name = "limit"), BindRequired, Range(2, int.MaxValue)]
        public int Limit { get; set; }
    }
}