using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTO.Game;
using Server.Models;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/games")]
    public class GamesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public GamesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllGames()
        {
            var games = await _context.Games.ToListAsync();

            var result = await _context.Games
     .Select(game => new GameDto
     {
         Id = game.Id,
         Title = game.Title,
         Description = game.Description,
         Price = game.Price,
         DiscountPercent = game.DiscountPercent,
         FinalPrice = game.DiscountPercent == null
             ? game.Price
             : game.Price - (game.Price * game.DiscountPercent.Value / 100),
         ReleaseDate = game.ReleaseDate,
         Developer = game.Developer,
         Publisher = game.Publisher,
         CoverImageUrl = game.CoverImageUrl
     })
     .ToListAsync();

            return Ok(result);
        }
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetGameById(int id)
        {
            var game = await _context.Games.FindAsync(id);

            if (game == null)
                return NotFound();

            var result = new GameDto
            {
                Id = game.Id,
                Title = game.Title,
                Description = game.Description,
                Price = game.Price,
                DiscountPercent = game.DiscountPercent,
                FinalPrice = game.DiscountPercent == null
                    ? game.Price
                    : game.Price - (game.Price * game.DiscountPercent.Value / 100),
                ReleaseDate = game.ReleaseDate,
                Developer = game.Developer,
                Publisher = game.Publisher,
                CoverImageUrl = game.CoverImageUrl
            };

            return Ok(result);
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateGame(CreateGameDto dto)
        {
            var game = new Game
            {
                Title = dto.Title,
                Description = dto.Description,
                Price = dto.Price,
                DiscountPercent = dto.DiscountPercent,
                ReleaseDate = dto.ReleaseDate,
                Developer = dto.Developer,
                Publisher = dto.Publisher,
                CoverImageUrl = dto.CoverImageUrl
            };

            _context.Games.Add(game);
            await _context.SaveChangesAsync();

            return Ok(game);
        }

        [HttpPut("update/{id}")]
        public async Task<IActionResult> UpdateGame(int id, CreateGameDto dto)
        {
            var game = await _context.Games.FindAsync(id);

            if (game == null)
                return NotFound();

            game.Title = dto.Title;
            game.Description = dto.Description;
            game.Price = dto.Price;
            game.DiscountPercent = dto.DiscountPercent;
            game.ReleaseDate = dto.ReleaseDate;
            game.Developer = dto.Developer;
            game.Publisher = dto.Publisher;
            game.CoverImageUrl = dto.CoverImageUrl;

            await _context.SaveChangesAsync();

            return Ok(game);
        }

        [HttpDelete("delete/{id}")]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var game = await _context.Games.FindAsync(id);

            if (game == null)
                return NotFound(new { message = "Game not found" });

            try
            {
                var cartItems = _context.CartItems.Where(c => c.GameId == id);
                _context.CartItems.RemoveRange(cartItems);

                await _context.SaveChangesAsync();

                _context.Games.Remove(game);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Game deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = "Error while deleting game",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }
    }
}