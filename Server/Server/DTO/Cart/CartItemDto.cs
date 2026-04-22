namespace Server.DTO.Cart
{
    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int GameId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
    }
}
