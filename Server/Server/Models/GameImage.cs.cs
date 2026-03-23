namespace Server.Models
{
    public class GameImage
    {
        public int Id { get; set; }

        public int GameId { get; set; }

        public Game Game { get; set; }

        public string ImageUrl { get; set; }


    }
}