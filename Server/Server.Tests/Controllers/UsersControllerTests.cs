using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Server.Controllers;
using Server.Data;
using Server.DTO.User;
using Server.Models;
using Server.Services;
using Xunit;

namespace Server.Tests.Controllers;

public class UsersControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public UsersControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    private UsersController CreateControllerWithUserId(int userId)
    {
        var controller = new UsersController(_context, null!);

        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString())
            },
            "TestAuth"));

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };

        return controller;
    }

    [Fact]
    public async Task GetMyProfile_ShouldReturnOk_WhenUserExists()
    {
   
        var user = new User
        {
            Username = "Yulia",
            Email = "yulia@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = "User",
            LvlAcc = 5,
            CreatedAt = DateTime.UtcNow,
            ProfilePictureUrl = null
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var controller = CreateControllerWithUserId(user.Id);

        var actionResult = await controller.GetMyProfile();

 
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetMyProfile_ShouldReturnUnauthorized_WhenClaimIsMissing()
    {
 
        var controller = new UsersController(_context, null!);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var actionResult = await controller.GetMyProfile();

        Assert.IsType<UnauthorizedResult>(actionResult);
    }

    [Fact]
    public async Task UpdateProfile_ShouldUpdateUsernameAndEmail_WhenDataIsValid()
    {
        var user = new User
        {
            Username = "OldName",
            Email = "old@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = "User",
            LvlAcc = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var controller = CreateControllerWithUserId(user.Id);

        var dto = new UpdateProfileDto
        {
            Username = "NewName",
            Email = "new@test.com"
        };

        var actionResult = await controller.UpdateProfile(dto);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);

        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.Equal("NewName", updatedUser!.Username);
        Assert.Equal("new@test.com", updatedUser.Email);
    }

    [Fact]
    public async Task UpdateProfile_ShouldReturnBadRequest_WhenEmailAlreadyExists()
    {
        var user1 = new User
        {
            Username = "User1",
            Email = "user1@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = "User",
            LvlAcc = 1,
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Username = "User2",
            Email = "user2@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            Role = "User",
            LvlAcc = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var controller = CreateControllerWithUserId(user1.Id);

        var dto = new UpdateProfileDto
        {
            Username = "UpdatedUser1",
            Email = "user2@test.com"
        };

        var actionResult = await controller.UpdateProfile(dto);
        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnOk_WhenCurrentPasswordIsCorrect()
    {
        var user = new User
        {
            Username = "User1",
            Email = "user1@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass"),
            Role = "User",
            LvlAcc = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var controller = CreateControllerWithUserId(user.Id);

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "oldpass",
            NewPassword = "newpass123",
            ConfirmNewPassword = "newpass123"
        };

        var actionResult = await controller.ChangePassword(dto);

        var okResult = Assert.IsType<OkObjectResult>(actionResult);

        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.NotNull(updatedUser);
        Assert.True(BCrypt.Net.BCrypt.Verify("newpass123", updatedUser!.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_WhenCurrentPasswordIsWrong()
    {
        var user = new User
        {
            Username = "User1",
            Email = "user1@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass"),
            Role = "User",
            LvlAcc = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var controller = CreateControllerWithUserId(user.Id);

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "wrongpass",
            NewPassword = "newpass123",
            ConfirmNewPassword = "newpass123"
        };

        var actionResult = await controller.ChangePassword(dto);

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnBadRequest_WhenPasswordsDoNotMatch()
    {
        var user = new User
        {
            Username = "User1",
            Email = "user1@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldpass"),
            Role = "User",
            LvlAcc = 1,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var controller = CreateControllerWithUserId(user.Id);

        var dto = new ChangePasswordDto
        {
            CurrentPassword = "oldpass",
            NewPassword = "newpass123",
            ConfirmNewPassword = "differentpass"
        };

        
        var actionResult = await controller.ChangePassword(dto);

        Assert.IsType<BadRequestObjectResult>(actionResult);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}