using System.ComponentModel.DataAnnotations;
using Api.Data.DataAnnotations;

namespace Api.Data.Payload
{
    public class PhonePayload
    {
        [Required, MobilePhoneNumber(nameof(Region))]
        public string Number { get; set; }

        [Required] public string Region { get; set; }
    }
}