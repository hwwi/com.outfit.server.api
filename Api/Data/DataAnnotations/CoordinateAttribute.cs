using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Data.DataAnnotations
{
    public class CoordinateAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            if (value == null)
                return true;
            float coordinate = (float)value;
            return 0 <= coordinate && coordinate <= 1;
        }
    }
}