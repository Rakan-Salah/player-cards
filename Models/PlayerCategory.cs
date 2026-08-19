using System.ComponentModel.DataAnnotations;

namespace PlayerCards.Models
{
    public class PlayerCategory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public ICollection<PlayerCard>? PlayerCards { get; set; }
    }
}
