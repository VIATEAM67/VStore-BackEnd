namespace Server.DTOs
{
    public class AchievementDto
    {
        public int Id { get; set; }
        public int GameId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsUnlocked { get; set; }

        public string? ImageUrl { get; set; }
        public DateTime? UnlockedAt { get; set; }
    }
}