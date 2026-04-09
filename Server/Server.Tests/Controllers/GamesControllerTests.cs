using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Server.Controllers;
using Server.Data;
using Server.DTO.Game;
using Server.Models;
using Xunit;

namespace Server.Tests.Controllers;

public class GamesControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly GamesController _controller;

    public GamesControllerTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();

        _controller = new GamesController(_context);
    }

    [Fact]
    public async Task GetAllGames_ShouldReturnOk_WithGameDtos()
    {
        _context.Games.AddRange(
            new Game
            {
                Title = "Game 1",
                Description = "Desc 1",
                Price = 1000,
                DiscountPercent = 20,
                ReleaseDate = new DateTime(2024, 1, 1),
                Developer = "Dev 1",
                Publisher = "Pub 1",
                CoverImageUrl = "cover1.jpg"
            },
            new Game
            {
                Title = "Game 2",
                Description = "Desc 2",
                Price = 500,
                DiscountPercent = null,
                ReleaseDate = new DateTime(2024, 2, 1),
                Developer = "Dev 2",
                Publisher = "Pub 2",
                CoverImageUrl = "cover2.jpg"
            }
        );

        await _context.SaveChangesAsync();

 
        var actionResult = await _controller.GetAllGames();

        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var games = Assert.IsAssignableFrom<IEnumerable<GameDto>>(okResult.Value);

        Assert.Equal(2, games.Count());

        var first = games.First(g => g.Title == "Game 1");
        Assert.Equal(800, first.FinalPrice); 

        var second = games.First(g => g.Title == "Game 2");
        Assert.Equal(500, second.FinalPrice); 
    }

    [Fact]
    public async Task GetGameById_ShouldReturnOk_WhenGameExists()
    {
        var game = new Game
        {
            Title = "Cyber Test",
            Description = "Test description",
            Price = 1200,
            DiscountPercent = 25,
            ReleaseDate = new DateTime(2025, 1, 1),
            Developer = "CD Test",
            Publisher = "Pub Test",
            CoverImageUrl = "cyber.jpg"
        };

        _context.Games.Add(game);
        await _context.SaveChangesAsync();

 
        var actionResult = await _controller.GetGameById(game.Id);


        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var dto = Assert.IsType<GameDto>(okResult.Value);

        Assert.Equal(game.Id, dto.Id);
        Assert.Equal("Cyber Test", dto.Title);
        Assert.Equal(900, dto.FinalPrice);
    }

    [Fact]
    public async Task GetGameById_ShouldReturnNotFound_WhenGameDoesNotExist()
    {
     
        var actionResult = await _controller.GetGameById(9999);

   
        Assert.IsType<NotFoundResult>(actionResult);
    }

    [Fact]
    public async Task CreateGame_ShouldCreateAndReturnGame()
    {
  
        var dto = new CreateGameDto
        {
            Title = "New Game",
            Description = "New description",
            Price = 1500,
            DiscountPercent = 10,
            ReleaseDate = new DateTime(2025, 5, 1),
            Developer = "New Dev",
            Publisher = "New Pub",
            CoverImageUrl = "new-cover.jpg"
        };

   
        var actionResult = await _controller.CreateGame(dto);

   
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var createdGame = Assert.IsType<Game>(okResult.Value);

        Assert.True(createdGame.Id > 0);
        Assert.Equal("New Game", createdGame.Title);

        var gameInDb = await _context.Games.FindAsync(createdGame.Id);
        Assert.NotNull(gameInDb);
        Assert.Equal("New Dev", gameInDb!.Developer);
    }

    [Fact]
    public async Task UpdateGame_ShouldUpdateAndReturnGame_WhenGameExists()
    {
       
        var game = new Game
        {
            Title = "Old Title",
            Description = "Old Desc",
            Price = 700,
            DiscountPercent = null,
            ReleaseDate = new DateTime(2024, 1, 1),
            Developer = "Old Dev",
            Publisher = "Old Pub",
            CoverImageUrl = "old.jpg"
        };

        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        var dto = new CreateGameDto
        {
            Title = "Updated Title",
            Description = "Updated Desc",
            Price = 900,
            DiscountPercent = 15,
            ReleaseDate = new DateTime(2024, 2, 2),
            Developer = "Updated Dev",
            Publisher = "Updated Pub",
            CoverImageUrl = "updated.jpg"
        };

        
        var actionResult = await _controller.UpdateGame(game.Id, dto);

       
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        var updatedGame = Assert.IsType<Game>(okResult.Value);

        Assert.Equal("Updated Title", updatedGame.Title);
        Assert.Equal("Updated Dev", updatedGame.Developer);

        var gameInDb = await _context.Games.FindAsync(game.Id);
        Assert.NotNull(gameInDb);
        Assert.Equal("Updated Pub", gameInDb!.Publisher);
        Assert.Equal(15, gameInDb.DiscountPercent);
    }

    [Fact]
    public async Task UpdateGame_ShouldReturnNotFound_WhenGameDoesNotExist()
    {
       
        var dto = new CreateGameDto
        {
            Title = "Updated Title",
            Description = "Updated Desc",
            Price = 900,
            DiscountPercent = 15,
            ReleaseDate = new DateTime(2024, 2, 2),
            Developer = "Updated Dev",
            Publisher = "Updated Pub",
            CoverImageUrl = "updated.jpg"
        };

        
        var actionResult = await _controller.UpdateGame(9999, dto);

     
        Assert.IsType<NotFoundResult>(actionResult);
    }

    [Fact]
    public async Task DeleteGame_ShouldReturnOk_WhenGameExists()
    {
     
        var game = new Game
        {
            Title = "Delete Me",
            Description = "To delete",
            Price = 300,
            DiscountPercent = null,
            ReleaseDate = new DateTime(2024, 3, 3),
            Developer = "Delete Dev",
            Publisher = "Delete Pub",
            CoverImageUrl = "delete.jpg"
        };

        _context.Games.Add(game);
        await _context.SaveChangesAsync();

   
        var actionResult = await _controller.DeleteGame(game.Id);

     
        var okResult = Assert.IsType<OkObjectResult>(actionResult);
        Assert.Null(await _context.Games.FindAsync(game.Id));
    }

    [Fact]
    public async Task DeleteGame_ShouldReturnNotFound_WhenGameDoesNotExist()
    {
      
        var actionResult = await _controller.DeleteGame(9999);

      
        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
        Assert.NotNull(notFound.Value);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}