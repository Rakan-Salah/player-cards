using PlayerCards.Entities;

namespace PlayerCards.Models
{
    public class UserAnnouncement
    {
        public int Id { get; set; }

        public int UserAccountId { get; set; }
        public UserAccount UserAccount { get; set; }

        public int AnnouncementId { get; set; }
        public Announcement Announcement { get; set; }

        public bool IsRead { get; set; } = false;
    }
}
