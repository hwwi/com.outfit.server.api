using System.ComponentModel.DataAnnotations;
using Api.Data.DataAnnotations;

namespace Api.Data.Args
{
    public class PersonPostArgs
    {
        [PersonName]
        public string Name { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [MobileE164PhoneNumber]
        public string? PhoneNumber { get; set; }
        
        public long VerificationId { get; set; }

        [PersonPassword]
        public string Password { get; set; }

        [Compare("Password")]
        public string ConfirmPassword { get; set; }
    }
}