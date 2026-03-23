using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTOs;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishListController : ControllerBase
    {
        private readonly AppDbContext _context;

        public WishListController(AppDbContext context)
        {
            _context = context;
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.Parse(userIdClaim!);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<WishListItemDto>>> GetMyWishList()
        {
            int userId = GetUserId();

            var items = await _context.WishLists
                .Where(w => w.UserId == userId)
                .Include(w => w.Game)
                .Select(w => new WishListItemDto
                {
                    GameId = w.GameId,
                    Title = w.Game.Title,
                    Price = w.Game.Price,
                    DiscountPercent = w.Game.DiscountPercent,
                    CoverImageUrl = w.Game.CoverImageUrl,
                    AddedAt = w.AddedAt
                })
                .ToListAsync();

            return Ok(items);
        }

        [HttpPost("{gameId}")]
        public async Task<IActionResult> AddToWishList(int gameId)
        {
            int userId = GetUserId();

            var gameExists = await _context.Games.AnyAsync(g => g.Id == gameId);
            if (!gameExists)
                return NotFound(new { message = "Гра не знайдена" });

            var exists = await _context.WishLists.AnyAsync(w => w.UserId == userId && w.GameId == gameId);
            if (exists)
                return BadRequest(new { message = "Гра вже є в wishlist" });

            var item = new WishList
            {
                UserId = userId,
                GameId = gameId,
                AddedAt = DateTime.UtcNow
            };

            _context.WishLists.Add(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Гру додано в wishlist" });
        }

        [HttpDelete("{gameId}")]
        public async Task<IActionResult> RemoveFromWishList(int gameId)
        {
            int userId = GetUserId();

            var item = await _context.WishLists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.GameId == gameId);

            if (item == null)
                return NotFound(new { message = "Гри немає в wishlist" });

            _context.WishLists.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Гру видалено з wishlist" });
        }

        [HttpGet("contains/{gameId}")]
        public async Task<IActionResult> IsInWishList(int gameId)
        {
            int userId = GetUserId();

            var exists = await _context.WishLists
                .AnyAsync(w => w.UserId == userId && w.GameId == gameId);

            return Ok(new { isInWishList = exists });
        }
    }
}