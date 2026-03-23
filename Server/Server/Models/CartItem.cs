namespace Server.Models
{
    public class CartItem
    {
        public int Id { get; set; }
        public int CartId { get; set; }
        public int GameId { get; set; }
        public decimal PriceAtAdd { get; set; }

        public Cart Cart { get; set; } = null!;
        public Game Game { get; set; } = null!;
    }
}
