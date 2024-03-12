using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Data.DataAnnotations
{
    public class CommentTextAttribute : StringLengthAttribute
    {
        public CommentTextAttribute() : base(300)
        {
            MinimumLength = 1;
        }
    }
}
