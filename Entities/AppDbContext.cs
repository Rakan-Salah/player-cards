using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using PlayerCards.Entities;
using PlayerCards.Models;

namespace PlayerCards.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<UserAccount> UserAccounts { get; set; }
        public DbSet<PlayerCard> PlayerCards { get; set; }
        public DbSet<PlayerCategory> PlayerCategories { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<LikedItem> LikedItems { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<UserAnnouncement> UserAnnouncements { get; set; }
        public DbSet<Tag> Tags { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Fix Price precision warning

            modelBuilder.Entity<PlayerCard>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<UserAccount>()
                .HasMany(u => u.CartItems).WithOne(ci => ci.UserAccount)
                .HasForeignKey(x => x.UserAccountId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UserAccount>()
                .HasMany(u => u.LikedItems).WithOne(ci => ci.UserAccount)
                .HasForeignKey(x => x.UserAccountId)
                .OnDelete(DeleteBehavior.NoAction);
            ;

            modelBuilder.Entity<UserAccount>()
                .HasMany(u => u.PlayerCards).WithOne(ci => ci.UserAccount)
                .HasForeignKey(x => x.UserAccountId)
                              .OnDelete(DeleteBehavior.NoAction);
            ;
            modelBuilder.Entity<PlayerCard>()
                .HasMany(pc => pc.CartItems);


            modelBuilder.Entity<UserAnnouncement>()
                .HasOne(ua => ua.UserAccount)
                .WithMany(u => u.UserAnnouncements)
                .HasForeignKey(ua => ua.UserAccountId);

            modelBuilder.Entity<UserAnnouncement>()
                .HasOne(ua => ua.Announcement)
                .WithMany(a => a.UserAnnouncements)
                .HasForeignKey(ua => ua.AnnouncementId);

            modelBuilder.Entity<PlayerCard>()
                .HasMany(pc => pc.Tags)
                .WithMany(t => t.PlayerCards)
                .UsingEntity(j => j.ToTable("PlayerCardTags"));
        }

    }
}
