using System.ComponentModel.DataAnnotations;

namespace PlayerCards.Models
{
    public class Registration
    {
        public string? FirstName { get; set; }
        [Required]
        public string? LastName { get; set; }
        [Required]
        public string? Email { get; set; }
        [Required]
        public string? UserName { get; set; }
        [Required]
        public string? Password { get; set; }
        [Compare("Password")]
        public string? ConfirmPassword { get; set; }

        public string? Address { get; set; }
        public string  ? PhoneNumber { get; set; }
    }
}
