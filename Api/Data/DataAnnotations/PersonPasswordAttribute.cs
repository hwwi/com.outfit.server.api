using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Data.DataAnnotations
{
    public class PersonPasswordAttribute : StringLengthAttribute
    {
        public PersonPasswordAttribute() : base(20)
        {
            MinimumLength = 8;
        }
    }
}
