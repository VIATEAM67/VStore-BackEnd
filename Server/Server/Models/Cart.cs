namespace Server.Models
{
    public class Cart
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; } = null!;
        public List<CartItem> CartItems { get; set; } = new();
    }
}
