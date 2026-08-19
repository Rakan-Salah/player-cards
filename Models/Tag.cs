using System.ComponentModel.DataAnnotations;

namespace PlayerCards.Models
{
    public class Tag
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;

        public ICollection<PlayerCard>? PlayerCards { get; set; } = new List<PlayerCard>();
    }
}
