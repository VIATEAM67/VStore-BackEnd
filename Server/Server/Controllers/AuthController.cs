using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.DTO.Auth;
using Server.Models;
using Server.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly FileLogger _logger;

        public AuthController(AppDbContext context, IConfiguration config, FileLogger logger)
        {
            _context = context;
            _config = config;
            _logger = logger;
        }

        private async Task<string> GenerateUsername()
        {
            int count = await _context.Users.CountAsync();
            return $"Nickname_{count + 1}";
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDTO dto)
        {
            _logger.Log("INFO", $"Запрос на регистрацию. Email: {dto.Email}");
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (await _context.Users.AnyAsync(u => u.Email == dto.Email))
            {
                _logger.Log("WARN", $"Попытка регистрации с существующим email: {dto.Email}");
                return BadRequest(new { message = "Пользователь с таким Email уже существует" });
            }
                

            var username = await GenerateUsername();

            var user = new User
            {
                Username = username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "User",
                LvlAcc = 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            _logger.Log("INFO", $"Пользователь зарегистрирован: id={user.Id}");

            var token = GenerateJwtToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role,
                    user.LvlAcc
                }
            });


        }

     
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDTO dto)
        {
            _logger.Log("INFO", $"Попытка входа: {dto.Email}");
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                _logger.Log("WARN", $"Неудачная попытка входа. Email: {dto.Email}");
                return Unauthorized(new { message = "Неверный Email или пароль" });
            }
            
            var token = GenerateJwtToken(user);
            _logger.Log("INFO", $"Успешный вход: id={user.Id}");

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role,
                    user.LvlAcc
                }
            });

        }

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(6),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}