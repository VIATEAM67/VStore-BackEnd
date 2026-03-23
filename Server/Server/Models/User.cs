using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string Username { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        [Required]
        public string Role { get; set; } = "User";

        public int LvlAcc { get; set; } = 1;

        public DateTime CreatedAt { get; set; }

        public string? ProfilePictureUrl { get; set; } = "/images/default-avatar.jpg";

        public List<UserLibrary> Library { get; set; } = new();

        public ICollection<WishList> WishListItems { get; set; } = new List<WishList>();
        public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    }
}