using System.ComponentModel.DataAnnotations;

namespace Server.Models
{
    public class Genre
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; }

        public ICollection<GameGenre>? GameGenres { get; set; }
    }
}
