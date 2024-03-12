using Api.Data.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Data.Args
{
    public class AuthPostRefreshTokenArgs
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}