using Api.Data.DataAnnotations;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Data.Args
{
    public class AuthPostTokenArgs
    {
        [Required] public string PhoneOrEmailOrName { get; set; }
        [Required, PersonPassword] public string Password { get; set; }
        public string? Region { get; set; }
        public NotificationPostCloudMessagingTokenArgs? CloudMessagingTokens { get; set; }
    }
}