using System.ComponentModel.DataAnnotations;

namespace PlayerCards.Models
{
    public class ForgotPasswordViewModel
    {
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }

}
