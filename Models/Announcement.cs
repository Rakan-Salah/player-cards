using PlayerCards.Models;

public class Announcement
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    //Relationship
    public ICollection<UserAnnouncement> UserAnnouncements { get; set; } = new List<UserAnnouncement>();
}
