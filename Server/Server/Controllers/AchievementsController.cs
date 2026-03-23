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
    public class AchievementsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AchievementsController(AppDbContext context)
        {
            _context = context;
        }

        private int? GetUserIdOrNull()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(claim) || !int.TryParse(claim, out var userId))
                return null;

            return userId;
        }

        [HttpGet("game/{gameId}")]
        public async Task<ActionResult<IEnumerable<AchievementDto>>> GetGameAchievements(int gameId)
        {
            var gameExists = await _context.Games.AnyAsync(g => g.Id == gameId);
            if (!gameExists)
                return NotFound(new { message = "Гру не знайдено" });

            int? userId = GetUserIdOrNull();

            var achievements = await _context.Achievements
                .Where(a => a.GameId == gameId)
                .OrderBy(a => a.Id)
                .Select(a => new AchievementDto
                {
                    Id = a.Id,
                    GameId = a.GameId,
                    Title = a.Title,
                    Description = a.Description,
                    ImageUrl = a.ImageUrl,
                    IsUnlocked = userId != null && a.UserAchievements.Any(ua => ua.UserId == userId),
                    UnlockedAt = userId != null
                        ? a.UserAchievements
                            .Where(ua => ua.UserId == userId)
                            .Select(ua => (DateTime?)ua.UnlockedAt)
                            .FirstOrDefault()
                        : null
                })
                .ToListAsync();

            return Ok(achievements);
        }

        [Authorize]
        [HttpPost("unlock/{achievementId}")]
        public async Task<IActionResult> UnlockAchievement(int achievementId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Некоректний користувач" });

            var achievement = await _context.Achievements
                .FirstOrDefaultAsync(a => a.Id == achievementId);

            if (achievement == null)
                return NotFound(new { message = "Ачивку не знайдено" });

            var alreadyUnlocked = await _context.UserAchievements
                .AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId);

            if (alreadyUnlocked)
                return BadRequest(new { message = "Ачивка вже відкрита" });

            var userHasGame = await _context.UserLibrary
                .AnyAsync(ul => ul.UserId == userId && ul.GameId == achievement.GameId);

            if (!userHasGame)
                return BadRequest(new { message = "Користувач не має цієї гри в бібліотеці" });

            var userAchievement = new UserAchievement
            {
                UserId = userId,
                AchievementId = achievementId,
                UnlockedAt = DateTime.UtcNow
            };

            _context.UserAchievements.Add(userAchievement);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Ачивку відкрито" });
        }

        [Authorize]
        [HttpGet("my")]
        public async Task<ActionResult<IEnumerable<AchievementDto>>> GetMyAchievements()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { message = "Некоректний користувач" });

            var achievements = await _context.UserAchievements
                .Where(ua => ua.UserId == userId)
                .Include(ua => ua.Achievement)
                .OrderByDescending(ua => ua.UnlockedAt)
                .Select(ua => new AchievementDto
                {
                    Id = ua.Achievement.Id,
                    GameId = ua.Achievement.GameId,
                    Title = ua.Achievement.Title,
                    Description = ua.Achievement.Description,
                    ImageUrl = ua.Achievement.ImageUrl,
                    IsUnlocked = true,
                    UnlockedAt = ua.UnlockedAt
                })
                .ToListAsync();

            return Ok(achievements);
        }
    }
}
