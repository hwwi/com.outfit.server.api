using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace Api.Configuration
{
    public class SecuritySettings
    {
        public string JwtKey { get; set; }
        public string GoogleApplicationCredentialsJson { get; set; }

        public SymmetricSecurityKey SymmetricJwtKey => new SymmetricSecurityKey(Encoding.ASCII.GetBytes(JwtKey));
    }
}