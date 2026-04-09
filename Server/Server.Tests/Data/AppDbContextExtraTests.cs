using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Xunit;

namespace Server.Tests.Data;

public class AppDbContextExtraTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;

    public AppDbContextExtraTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Can_Save_Game_With_RequiredFields()
    {
        
        var game = new Game
        {
            Title = "DB Game",
            Description = "DB Description",
            Price = 1500,
            DiscountPercent = 10,
            ReleaseDate = new DateTime(2025, 1, 1),
            Developer = "DB Dev",
            Publisher = "DB Pub",
            CoverImageUrl = "db-cover.jpg"
        };

       
        _context.Games.Add(game);
        await _context.SaveChangesAsync();

       
        var saved = await _context.Games.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("DB Dev", saved!.Developer);
    }

    [Fact]
    public async Task Can_Save_Game_With_Images()
    {
        
        var game = new Game
        {
            Title = "Game With Images",
            Description = "Has screenshots",
            Price = 1200,
            DiscountPercent = null,
            ReleaseDate = new DateTime(2025, 1, 1),
            Developer = "Dev",
            Publisher = "Pub",
            CoverImageUrl = "cover.jpg"
        };

        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        var image1 = new GameImage { GameId = game.Id, ImageUrl = "1.jpg" };
        var image2 = new GameImage { GameId = game.Id, ImageUrl = "2.jpg" };

       
        _context.GameImages.AddRange(image1, image2);
        await _context.SaveChangesAsync();

        
        var images = await _context.GameImages.Where(x => x.GameId == game.Id).ToListAsync();
        Assert.Equal(2, images.Count);
    }

    [Fact]
    public async Task Can_Save_Game_With_Genres()
    {
       
        var genre1 = new Genre { Name = "Action" };
        var genre2 = new Genre { Name = "RPG" };

        _context.Genres.AddRange(genre1, genre2);
        await _context.SaveChangesAsync();

        var game = new Game
        {
            Title = "Genre Game",
            Description = "Genre Description",
            Price = 1800,
            DiscountPercent = 15,
            ReleaseDate = new DateTime(2025, 1, 1),
            Developer = "Genre Dev",
            Publisher = "Genre Pub",
            CoverImageUrl = "genre.jpg"
        };

        _context.Games.Add(game);
        await _context.SaveChangesAsync();

        var link1 = new GameGenre { GameId = game.Id, GenreId = genre1.Id };
        var link2 = new GameGenre { GameId = game.Id, GenreId = genre2.Id };

   
        _context.GameGenres.AddRange(link1, link2);
        await _context.SaveChangesAsync();


        var links = await _context.GameGenres.Where(x => x.GameId == game.Id).ToListAsync();
        Assert.Equal(2, links.Count);
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Close();
        _connection.Dispose();
    }
}