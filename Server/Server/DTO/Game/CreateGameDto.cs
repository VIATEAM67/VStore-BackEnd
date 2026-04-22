using System.ComponentModel.DataAnnotations;

namespace Server.DTO.Game
{
    public class CreateGameDto
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Range(0, 1000)]
        public decimal Price { get; set; }

        [Range(0, 100)]
        public int? DiscountPercent { get; set; }

        public DateTime ReleaseDate { get; set; }

        [Required]
        public string Developer { get; set; }

        [Required]
        public string Publisher { get; set; }

        public string? CoverImageUrl { get; set; }
    }
}