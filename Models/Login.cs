using System.ComponentModel.DataAnnotations;

namespace PlayerCards.Models
{
    public class Login
    {
        [Required]
        public string? UserNameOrEmail { get; set; }
        [Required]
        public string? Password { get; set; }

        public string? RecaptchaToken { get; set; }
    }
}
