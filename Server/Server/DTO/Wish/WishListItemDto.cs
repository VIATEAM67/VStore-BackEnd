namespace Server.DTOs
{
    public class WishListItemDto
    {
        public int GameId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int? DiscountPercent { get; set; }
        public string CoverImageUrl { get; set; } = string.Empty;
        public DateTime AddedAt { get; set; }
    }
}