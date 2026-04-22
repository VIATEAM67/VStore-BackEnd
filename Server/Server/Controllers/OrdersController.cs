using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FileLogger _logger;

        public OrdersController(AppDbContext context, FileLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (int.TryParse(userIdClaim, out var userId))
                return userId;

            return null;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder()
        {
            var userId = GetCurrentUserId();

            _logger.Log("INFO", $"Запрос на создание заказа. UserId={(userId?.ToString() ?? "null")}");

            if (userId == null)
            {
                _logger.Log("WARN", "Неавторизованная попытка создания заказа.");
                return Unauthorized();
            }

            var cart = await _context.Carts
                .Include(c => c.CartItems)
                .ThenInclude(ci => ci.Game)
                .FirstOrDefaultAsync(c => c.UserId == userId.Value);

            if (cart == null)
            {
                _logger.Log("WARN", $"Корзина не найдена при создании заказа. UserId={userId.Value}");
                return BadRequest(new { message = "Кошик порожній." });
            }

            if (!cart.CartItems.Any())
            {
                _logger.Log("WARN", $"Попытка создать заказ с пустой корзиной. UserId={userId.Value}, CartId={cart.Id}");
                return BadRequest(new { message = "Кошик порожній." });
            }

            var totalAmount = cart.CartItems.Sum(ci => ci.PriceAtAdd);

            _logger.Log("INFO", $"Начато создание заказа. UserId={userId.Value}, CartId={cart.Id}, ItemsCount={cart.CartItems.Count}, TotalAmount={totalAmount}");

            var order = new Order
            {
                UserId = userId.Value,
                TotalAmount = totalAmount,
                Status = "Paid",
                CreatedAt = DateTime.UtcNow,
                PaidAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.Log("INFO", $"Заказ сохранён. OrderId={order.Id}, UserId={userId.Value}, TotalAmount={totalAmount}");

            var orderItems = cart.CartItems.Select(ci => new OrderItem
            {
                OrderId = order.Id,
                GameId = ci.GameId,
                PricePaid = ci.PriceAtAdd
            }).ToList();

            _context.OrderItems.AddRange(orderItems);

            int addedToLibraryCount = 0;

            foreach (var item in cart.CartItems)
            {
                var exists = await _context.UserLibrary
                    .AnyAsync(x => x.UserId == userId.Value && x.GameId == item.GameId);

                if (!exists)
                {
                    _context.UserLibrary.Add(new UserLibrary
                    {
                        UserId = userId.Value,
                        GameId = item.GameId,
                        PurchaseDate = DateTime.UtcNow
                    });

                    addedToLibraryCount++;
                }
            }

            _context.CartItems.RemoveRange(cart.CartItems);

            await _context.SaveChangesAsync();

            _logger.Log(
                "INFO",
                $"Заказ успешно завершён. OrderId={order.Id}, UserId={userId.Value}, OrderItems={orderItems.Count}, AddedToLibrary={addedToLibraryCount}, CartCleared=true"
            );

            return Ok(new
            {
                message = "Замовлення успішно створено",
                orderId = order.Id,
                totalAmount
            });
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetCurrentUserId();

            _logger.Log("INFO", $"Запрос на получение заказов пользователя. UserId={(userId?.ToString() ?? "null")}");

            if (userId == null)
            {
                _logger.Log("WARN", "Неавторизованная попытка получить список заказов.");
                return Unauthorized();
            }

            var orders = await _context.Orders
                .Where(o => o.UserId == userId.Value)
                .Include(o => o.Items)
                .ThenInclude(i => i.Game)
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new
                {
                    o.Id,
                    o.TotalAmount,
                    o.Status,
                    o.CreatedAt,
                    o.PaidAt,
                    Items = o.Items.Select(i => new
                    {
                        i.GameId,
                        i.Game.Title,
                        i.PricePaid,
                        i.Game.CoverImageUrl
                    })
                })
                .ToListAsync();

            _logger.Log("INFO", $"Список заказов загружен. UserId={userId.Value}, OrdersCount={orders.Count}");

            return Ok(orders);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrderById(int id)
        {
            var userId = GetCurrentUserId();

            _logger.Log("INFO", $"Запрос на получение заказа по ID. UserId={(userId?.ToString() ?? "null")}, OrderId={id}");

            if (userId == null)
            {
                _logger.Log("WARN", $"Неавторизованная попытка получить заказ. OrderId={id}");
                return Unauthorized();
            }

            var order = await _context.Orders
                .Where(o => o.Id == id && o.UserId == userId.Value)
                .Include(o => o.Items)
                .ThenInclude(i => i.Game)
                .Select(o => new
                {
                    o.Id,
                    o.TotalAmount,
                    o.Status,
                    o.CreatedAt,
                    o.PaidAt,
                    Items = o.Items.Select(i => new
                    {
                        i.GameId,
                        i.Game.Title,
                        i.PricePaid,
                        i.Game.CoverImageUrl
                    })
                })
                .FirstOrDefaultAsync();

            if (order == null)
            {
                _logger.Log("WARN", $"Заказ не найден или не принадлежит пользователю. UserId={userId.Value}, OrderId={id}");
                return NotFound(new { message = "Замовлення не знайдено." });
            }

            _logger.Log("INFO", $"Заказ успешно получен. UserId={userId.Value}, OrderId={id}");

            return Ok(order);
        }
    }
}