namespace Server.Models
{
    public class UserLibrary
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int GameId { get; set; }

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public int PlaytimeMinutes { get; set; } = 0;

        public DateTime LastTimePlayed { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;

        public Game Game { get; set; } = null!;
    }
}
