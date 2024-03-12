using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Api.Data.Payload
{
    public class AuthPostTokenPayload
    {
        public long Id { get; set; }
        public string PhoneOrEmailOrName { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}