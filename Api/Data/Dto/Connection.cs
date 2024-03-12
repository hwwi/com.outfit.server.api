using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Data.Dto
{
    public class Connection<T>
    {
        [Required]
        public List<T> Edges { get; set; }

        [Required]
        public PageInfo PageInfo { get; set; }
    }

    public class PageInfo
    {
        public long? StartCursor { get; set; }
        public long? EndCursor { get; set; }

        [Required]
        public bool HasMorePage { get; set; }
    }
}