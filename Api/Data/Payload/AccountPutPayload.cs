using Api.Data.DataAnnotations;
using Api.Data.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Data.Args
{
    public class AccountPutPayload
    {
        public long PersonId { get; set; }
        public string Biography { get; set; }
    }
}