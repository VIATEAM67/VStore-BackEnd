using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Game
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(5000)]
        public string Description { get; set; }

        [Range(0, 5000)]
        public decimal Price { get; set; }

        [Range(0, 100)]
        public int? DiscountPercent { get; set; }

        public DateTime ReleaseDate { get; set; }

        [Required]
        [StringLength(200)]
        public string Developer { get; set; }

        [Required]
        [StringLength(200)]
        public string Publisher { get; set; }

        [Url]
        public string CoverImageUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<GameGenre>? GameGenres { get; set; }

        public ICollection<GameImage>? Images { get; set; }

        public List<UserLibrary> Owners { get; set; } = new();

        public ICollection<WishList> WishListUsers { get; set; } = new List<WishList>();
        public ICollection<Achievement> Achievements { get; set; } = new List<Achievement>();
    }
}
