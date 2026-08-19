using PlayerCards.Models;

namespace PlayerCards.Entities
{
    public class CartItem
    {
        public int Id { get; set; }
        public int UserAccountId { get; set; }
        public UserAccount? UserAccount { get; set; }

        public int PlayerCardId { get; set; }
        public PlayerCard PlayerCard { get; set; }
    }
}
