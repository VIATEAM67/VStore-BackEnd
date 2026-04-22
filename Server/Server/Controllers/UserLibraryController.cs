using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Services;
using System.Security.Claims;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/library")]
    public class UserLibraryController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FileLogger _logger;

        public UserLibraryController(AppDbContext context, FileLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<IActionResult> GetMyLibrary()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            _logger.Log("INFO", $"Запрос на получение библиотеки пользователя. ClaimUserId={userIdClaim ?? "null"}");

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                _logger.Log("WARN", "Не удалось определить пользователя при запросе библиотеки.");
                return Unauthorized();
            }

            var library = await _context.UserLibrary
                .Where(x => x.UserId == userId)
                .Include(x => x.Game)
                .Select(x => new
                {
                    x.GameId,
                    x.Game.Title,
                    x.Game.CoverImageUrl,
                    PlayTimeHours = x.PlaytimeMinutes / 60.0,
                    LastPlayed = x.LastTimePlayed
                })
                .ToListAsync();

            _logger.Log("INFO", $"Библиотека пользователя загружена. UserId={userId}, GamesCount={library.Count}");

            return Ok(library);
        }
    }
}