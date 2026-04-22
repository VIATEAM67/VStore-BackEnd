using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.DTO.Cart;
using Server.Models;
using System.Security.Claims;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/cart")]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }

        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Неможливо визначити користувача." });
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Game)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
            {
                var emptyCart = new CartDto
                {
                    CartId = 0,
                    UserId = userId.Value,
                    Items = new List<CartItemDto>(),
                    TotalAmount = 0
                };

                return Ok(emptyCart);
            }

            var cartDto = new CartDto
            {
                CartId = cart.Id,
                UserId = cart.UserId,
                Items = cart.CartItems.Select(ci => new CartItemDto
                {
                    CartItemId = ci.Id,
                    GameId = ci.GameId,
                    Title = ci.Game.Title,
                    Price = ci.PriceAtAdd,
                    CoverImageUrl = ci.Game.CoverImageUrl
                }).ToList(),
                TotalAmount = cart.CartItems.Sum(ci => ci.PriceAtAdd)
            };

            return Ok(cartDto);
        }

        [HttpPost("add/{gameId}")]
        public async Task<IActionResult> AddToCart(int gameId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Неможливо визначити користувача." });
            }

            var game = await _context.Games.FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
            {
                return NotFound(new { message = "Гру не знайдено." });
            }

            var alreadyOwned = await _context.UserLibrary
                .AnyAsync(ul => ul.UserId == userId.Value && ul.GameId == gameId);

            if (alreadyOwned)
            {
                return BadRequest(new { message = "Ця гра вже куплена користувачем." });
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = userId.Value,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            var existsInCart = cart.CartItems.Any(ci => ci.GameId == gameId);
            if (existsInCart)
            {
                return BadRequest(new { message = "Ця гра вже є у кошику." });
            }

            var cartItem = new CartItem
            {
                CartId = cart.Id,
                GameId = gameId,
                PriceAtAdd = game.Price
            };

            _context.CartItems.Add(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Гру додано до кошика." });
        }

        [HttpDelete("remove/{gameId}")]
        public async Task<IActionResult> RemoveFromCart(int gameId)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized(new { message = "Неможливо визначити користувача." });
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
            {
                return NotFound(new { message = "Кошик не знайдено." });
            }

            var cartItem = cart.CartItems.FirstOrDefault(ci => ci.GameId == gameId);
            if (cartItem == null)
            {
                return NotFound(new { message = "Гру не знайдено у кошику." });
            }

            _context.CartItems.Remove(cartItem);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Гру видалено з кошика." });
        }
    }
}