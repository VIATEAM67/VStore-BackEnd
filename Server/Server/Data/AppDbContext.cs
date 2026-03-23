using Microsoft.EntityFrameworkCore;
using Server.Models;

namespace Server.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();

        public DbSet<Game> Games { get; set; }
        public DbSet<Genre> Genres { get; set; }

        public DbSet<GameGenre> GameGenres { get; set; }

        public DbSet<GameImage> GameImages { get; set; }

        public DbSet<UserLibrary> UserLibrary { get; set; }

        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }

        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<WishList> WishLists { get; set; }
        public DbSet<Achievement> Achievements { get; set; }
        public DbSet<UserAchievement> UserAchievements { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GameGenre>()
                .HasKey(x => new { x.GameId, x.GenreId });

            modelBuilder.Entity<GameGenre>()
                .HasOne(x => x.Game)
                .WithMany(x => x.GameGenres)
                .HasForeignKey(x => x.GameId);

            modelBuilder.Entity<GameGenre>()
                .HasOne(x => x.Genre)
                .WithMany(x => x.GameGenres)
                .HasForeignKey(x => x.GenreId);

            //cart
            modelBuilder.Entity<CartItem>()
                .HasIndex(ci => new { ci.CartId, ci.GameId })
                .IsUnique();

            modelBuilder.Entity<Cart>()
                .HasOne(c => c.User)
                .WithMany()
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Cart)
                .WithMany(c => c.CartItems)
                .HasForeignKey(ci => ci.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Game)
                .WithMany()
                .HasForeignKey(ci => ci.GameId)
                .OnDelete(DeleteBehavior.Restrict);

            //order
            modelBuilder.Entity<UserLibrary>()
                .HasIndex(x => new { x.UserId, x.GameId })
                .IsUnique();

            //wishlist
            modelBuilder.Entity<WishList>()
                .HasKey(w => new { w.UserId, w.GameId });

            modelBuilder.Entity<WishList>()
                .HasOne(w => w.User)
                .WithMany(u => u.WishListItems)
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WishList>()
                .HasOne(w => w.Game)
                .WithMany(g => g.WishListUsers)
                .HasForeignKey(w => w.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            //achievements
            modelBuilder.Entity<Achievement>()
                .HasOne(a => a.Game)
                .WithMany(g => g.Achievements)
                .HasForeignKey(a => a.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAchievement>()
                .HasIndex(ua => new { ua.UserId, ua.AchievementId })
                .IsUnique();

            modelBuilder.Entity<UserAchievement>()
                .HasOne(ua => ua.User)
                .WithMany(u => u.UserAchievements)
                .HasForeignKey(ua => ua.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UserAchievement>()
                .HasOne(ua => ua.Achievement)
                .WithMany(a => a.UserAchievements)
                .HasForeignKey(ua => ua.AchievementId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
