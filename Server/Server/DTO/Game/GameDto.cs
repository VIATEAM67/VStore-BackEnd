namespace Server.DTO.Game
{
    public class GameDto
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string? Description { get; set; }

        public decimal Price { get; set; }

        public int? DiscountPercent { get; set; }

        public decimal FinalPrice { get; set; }

        public DateTime ReleaseDate { get; set; }

        public string? Developer { get; set; }

        public string? Publisher { get; set; }

        public string? CoverImageUrl { get; set; }
    }
}