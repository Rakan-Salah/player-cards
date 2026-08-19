using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using PlayerCards.Models; // so we can reference PlayerCard

namespace PlayerCards.Entities
{
    [Index(nameof(Email), IsUnique = true)]
    [Index(nameof(UserName), IsUnique = true)]
    public class UserAccount
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? FirstName { get; set; }
        [Required]
        public string? LastName { get; set; }
        [Required]
        public string? Email { get; set; }
        [Required]
        public string? UserName { get; set; }
        [Required]
        public string? Password { get; set; }
       
        public string? Address { get; set; }
        
        public string? PhoneNumber { get; set; }

        public string? Role { get; set; } ="User"; // Default role is User
        public bool IsActive { get; set; } = true;
        public string? AvatarPath { get; set; }

        // 🔹 Many-to-many: liked cards
        public ICollection<LikedItem>? LikedItems { get; set; }

        // 🔹 Many-to-many: cart
        public ICollection<CartItem>? CartItems { get; set; }

        public ICollection<PlayerCard> PlayerCards { get; set; }

        public string? ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        public ICollection<UserAnnouncement> UserAnnouncements { get; set; } = new List<UserAnnouncement>();
    }

}
