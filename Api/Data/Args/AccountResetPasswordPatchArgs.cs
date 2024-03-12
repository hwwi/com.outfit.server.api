using System.ComponentModel.DataAnnotations;
using Api.Data.DataAnnotations;

namespace Api.Data.Args
{
    public class AccountResetPasswordPatchArgs
    {
        [Required]
        public long verificationId { get; set; }

        [Required, PersonPassword]
        public string NewPassword { get; set; }

        [Required, Compare("NewPassword")]
        public string ConfirmPassword { get; set; }
    }
}