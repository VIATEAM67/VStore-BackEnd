using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Achievement
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Url]
        public string ImageUrl { get; set; }

        public Game Game { get; set; } = null!;
        public ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
    }
}