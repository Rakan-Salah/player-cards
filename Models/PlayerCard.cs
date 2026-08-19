using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PlayerCards.Entities; // so we can reference UserAccount

namespace PlayerCards.Models
{
    public class PlayerCard
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string? Name { get; set; }
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? ImagePath { get; set; }
        public string? Offer { get; set; }
        [NotMapped]
        public bool Isliked { get; set; }
        [NotMapped]
        public bool IsInCart { get; set; }
        public int? UserAccountId { get; set; }
        public UserAccount? UserAccount { get; set; }
        public ICollection<CartItem>? CartItems { get; set; }

        public int LikesCount { get; set; }   // already updated when user clicks "like"
        public int CartCount { get; set; }    // updated when user adds to cart

       
        public int? CategoryId { get; set; }
        // Link to category
        [ForeignKey("CategoryId")]
        public PlayerCategory? CategoryLink { get; set; }

        public ICollection<Tag>? Tags { get; set; } = new List<Tag>();

    }
}
