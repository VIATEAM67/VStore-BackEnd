namespace Server.Models
{
    public class WishList
    {
        public int UserId { get; set; }
        public int GameId { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}