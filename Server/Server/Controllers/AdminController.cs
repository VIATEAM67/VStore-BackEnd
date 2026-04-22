using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Models;
using Server.Services;
using System.Net;
using System.Text;

namespace Server.Controllers
{
    [Route("/admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly FileLogger _logger;

        private const string AdminUser = "admin";
        private const string AdminPass = "7777";
        private const string AdminCookieName = "admin_auth";

        private readonly Dictionary<string, string> genreTranslation = new()
        {
            { "Action", "Екшн" },
            { "Adventure", "Пригоди" },
            { "RPG", "Рольові ігри" },
            { "Simulation", "Симулятори" },
            { "Strategy", "Стратегії" },
            { "Sports", "Спортивні ігри" },
            { "Puzzle", "Головоломки" },
            { "Shooter", "Шутери" },
            { "Horror", "Хорор" },
            { "Racing", "Гонки" },
            { "MMO", "ММО" }
        };

        public AdminController(AppDbContext context, FileLogger logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("login")]
        public IActionResult LoginPage()
        {
            if (IsAuthorized())
                return Redirect("/admin");

            return Content(BuildLoginPage(), "text/html; charset=utf-8");
        }

        [HttpPost("login")]
        public IActionResult Login([FromForm] string? user, [FromForm] string? pass)
        {
            if (!IsValidAdminCredentials(user, pass))
            {
                _logger.Log("WARN", "Невдала спроба входу в адмін-панель");
                return Content(BuildLoginPage("Невірний логін або пароль"), "text/html; charset=utf-8");
            }

            Response.Cookies.Append(AdminCookieName, "true", new CookieOptions
            {
                HttpOnly = true,
                Secure = Request.IsHttps,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(8)
            });

            _logger.Log("INFO", "Успішний вхід в адмін-панель");
            return Redirect("/admin");
        }

        [HttpGet("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete(AdminCookieName);
            _logger.Log("INFO", "Вихід з адмін-панелі");
            return Redirect("/admin/login");
        }

        [HttpGet("")]
        public IActionResult Index(
            [FromQuery] bool added = false,
            [FromQuery] string? search = null,
            [FromQuery] string view = "list")
        {
            if (!IsAuthorized())
            {
                _logger.Log("WARN", "Спроба входу в адмін-панель без авторизації");
                return Redirect("/admin/login");
            }

            _logger.Log("INFO", "Відкрито адмін-панель");

            var genres = _context.Genres.AsNoTracking().ToList();
            var gameImages = _context.GameImages.AsNoTracking().ToList();

            var gamesQuery = _context.Games.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.Trim().ToLower();

                gamesQuery = gamesQuery.Where(g =>
                    g.Title.ToLower().Contains(searchLower) ||
                    g.Developer.ToLower().Contains(searchLower) ||
                    g.Publisher.ToLower().Contains(searchLower));
            }

            var games = gamesQuery.ToList();
            var genreOptions = BuildGenreOptions(genres);

            var currentView = view?.ToLower() == "grid" ? "grid" : "list";
            var safeSearch = search ?? "";
            var encodedSearch = Uri.EscapeDataString(safeSearch);

            var gamesHtml = currentView == "grid"
                ? BuildGamesGridView(games)
                : BuildGamesCards(games, gameImages);

            var contentClass = currentView == "grid" ? "games-grid covers-mode" : "games-grid";

            var html = $@"
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Адмін-панель</title>
    <link rel='stylesheet' href='/css/admin.css?v=7'>
    <script>
        function openModal() {{
            document.getElementById('gameModal').classList.add('show');
            document.body.classList.add('modal-open');
        }}

        function closeModal() {{
            document.getElementById('gameModal').classList.remove('show');
            document.body.classList.remove('modal-open');
        }}

        function addScreenshotUrl() {{
            const container = document.getElementById('screenshots');
            const wrapper = document.createElement('div');
            wrapper.className = 'extra-file';

            wrapper.innerHTML = `
                <input type='text' name='screenshotUrls' placeholder='https://...' />
                <button type='button' class='btn small danger' onclick='removeScreenshotField(this)'>Видалити</button>
            `;

            container.appendChild(wrapper);
        }}

        function removeScreenshotField(button) {{
            button.parentElement.remove();
        }}

        window.onload = function() {{
            var added = {(added ? "true" : "false")};
            if (added) {{
                var toast = document.getElementById('toast');
                if (toast) {{
                    toast.classList.add('show');
                    setTimeout(function() {{
                        toast.classList.remove('show');
                    }}, 3000);
                }}
            }}
        }}

        window.onclick = function(event) {{
            var modal = document.getElementById('gameModal');
            if (event.target === modal) {{
                closeModal();
            }}
        }}

        let searchTimeout;
        let lastSubmittedValue = '';

        window.addEventListener('DOMContentLoaded', function () {{
            const searchInput = document.getElementById('searchInput');
            const searchForm = document.getElementById('searchForm');

            if (searchInput && searchForm) {{
                lastSubmittedValue = searchInput.value.trim().toLowerCase();

                searchInput.addEventListener('input', function () {{
                    clearTimeout(searchTimeout);

                    searchTimeout = setTimeout(function () {{
                        const currentValue = searchInput.value.trim().toLowerCase();

                        if (currentValue !== lastSubmittedValue) {{
                            searchForm.submit();
                        }}
                    }}, 700);
                }});
            }}
        }});
    </script>
</head>
<body>
    <div class='admin-page'>
        <div class='background-blur blur-1'></div>
        <div class='background-blur blur-2'></div>

        <div class='admin-shell'>
            <div class='topbar glass-panel'>
                <div class='topbar-left'>
                    <div class='panel-badge'>ПАНЕЛЬ КЕРУВАННЯ</div>
                    <h1>Адмін-панель</h1>
                    <p class='panel-subtitle'>
                        Керуйте іграми, редагуйте контент та швидко переглядайте інформацію про кожну гру
                    </p>
                </div>

                <div class='topbar-actions'>
                    <form method='get' action='/admin' class='admin-toolbar' id='searchForm'>
                        <input type='hidden' name='view' value='{currentView}' />

                        <input type='text'
                               id='searchInput'
                               name='search'
                               value='{Encode(safeSearch)}'
                               placeholder='Пошук гри...'
                               class='search-input'
                               autocomplete='off' />
                    </form>

                    <div class='toolbar-row'>
                        <div class='view-switch'>
                            <a class='btn {(currentView == "list" ? "primary" : "secondary")}'
                               href='/admin?search={encodedSearch}&view=list'>Список</a>

                            <a class='btn {(currentView == "grid" ? "primary" : "secondary")}'
                               href='/admin?search={encodedSearch}&view=grid'>Обкладинки</a>
                        </div>

                        <button class='btn primary' type='button' onclick='openModal()'>Додати нову гру</button>
                        <a class='btn secondary' href='/admin/logout'>Вийти</a>
                        <a class='btn secondary' href='/'>На головну</a>
                    </div>
                </div>
            </div>

            <div id='toast' class='toast'>Гру успішно додано!</div>

            <div class='{contentClass}'>
                {gamesHtml}
            </div>
        </div>
    </div>

    <div id='gameModal' class='modal'>
        <div class='modal-content glass-panel'>
            <div class='modal-header'>
                <div>
                    <div class='panel-badge'>НОВА ГРА</div>
                    <h2>Додати нову гру</h2>
                </div>
                <button class='icon-close' type='button' onclick='closeModal()'>×</button>
            </div>

            <form method='post' action='/admin/add' class='admin-form'>
                <div class='form-grid'>
                    <div class='form-group'>
                        <label>Назва гри</label>
                        <input type='text' name='title' placeholder='Назва гри' required />
                    </div>

                    <div class='form-group'>
                        <label>Ціна (₴)</label>
                        <input type='number' step='0.01' name='price' placeholder='Ціна' required />
                    </div>

                    <div class='form-group'>
                        <label>Дата виходу</label>
                        <input type='date' name='releaseDate' required />
                    </div>

                    <div class='form-group full'>
                        <label>Опис гри</label>
                        <textarea name='description' placeholder='Короткий опис гри' required></textarea>
                    </div>

                    <div class='form-group'>
                        <label>Знижка (%)</label>
                        <input type='number' name='discountPercent' placeholder='Наприклад, 15' />
                    </div>

                    <div class='form-group'>
                        <label>Розробник</label>
                        <input type='text' name='developer' placeholder='Розробник' required />
                    </div>

                    <div class='form-group'>
                        <label>Видавець</label>
                        <input type='text' name='publisher' placeholder='Видавець' required />
                    </div>

                    <div class='form-group full'>
                        <label>URL обкладинки</label>
                        <input type='text' name='coverUrl' placeholder='https://...' />
                    </div>

                    <div class='form-group full'>
                        <label>Скріншоти (URL)</label>
                        <div id='screenshots' class='screenshots-upload'>
                            <div class='extra-file'>
                                <input type='text' name='screenshotUrls' placeholder='https://...' />
                            </div>
                        </div>
                        <button type='button' class='btn small secondary' onclick='addScreenshotUrl()'>Додати ще URL</button>
                    </div>

                    <div class='form-group full'>
                        <label>Жанри</label>
                        <select name='genreIds' multiple required class='multi-select'>
                            {genreOptions}
                        </select>
                    </div>
                </div>

                <div class='form-actions'>
                    <button type='submit' class='btn primary'>Додати гру</button>
                    <button type='button' class='btn secondary' onclick='closeModal()'>Скасувати</button>
                </div>
            </form>
        </div>
    </div>
</body>
</html>";

            return Content(html, "text/html; charset=utf-8");
        }

        [HttpPost("add")]
        public IActionResult AddGame(
            [FromForm] string title,
            [FromForm] string description,
            [FromForm] decimal price,
            [FromForm] DateTime releaseDate,
            [FromForm] int? discountPercent,
            [FromForm] string developer,
            [FromForm] string publisher,
            [FromForm] int[] genreIds,
            [FromForm] string? coverUrl,
            [FromForm] List<string>? screenshotUrls)
        {
            if (!IsAuthorized())
            {
                _logger.Log("WARN", "Спроба додати гру без авторизації");
                return Redirect("/admin/login");
            }

            try
            {
                var finalCoverUrl = !string.IsNullOrWhiteSpace(coverUrl)
                    ? coverUrl.Trim()
                    : "/images/default_cover.png";

                var game = new Game
                {
                    Title = title,
                    Description = description,
                    Price = price,
                    DiscountPercent = discountPercent,
                    Developer = developer,
                    Publisher = publisher,
                    CoverImageUrl = finalCoverUrl,
                    ReleaseDate = DateTime.SpecifyKind(releaseDate, DateTimeKind.Utc)
                };

                _context.Games.Add(game);
                _context.SaveChanges();

                foreach (var genreId in genreIds)
                {
                    _context.GameGenres.Add(new GameGenre
                    {
                        GameId = game.Id,
                        GenreId = genreId
                    });
                }

                if (screenshotUrls != null)
                {
                    foreach (var url in screenshotUrls)
                    {
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            _context.GameImages.Add(new GameImage
                            {
                                GameId = game.Id,
                                ImageUrl = url.Trim()
                            });
                        }
                    }
                }

                _context.SaveChanges();

                _logger.Log("INFO", $"Додано гру: id={game.Id}, title={game.Title}");
                return Redirect("/admin?added=true");
            }
            catch (Exception ex)
            {
                _logger.Log("ERROR", $"Помилка при додаванні гри '{title}': {ex.Message}");
                return Content($"<h1>Помилка при додаванні гри</h1><p>{Encode(ex.Message)}</p>", "text/html; charset=utf-8");
            }
        }

        [HttpPost("achievement/add")]
        public IActionResult AddAchievement(
            [FromForm] int gameId,
            [FromForm] string title,
            [FromForm] string description,
            [FromForm] string ImageUrl)
        {
            if (!IsAuthorized())
            {
                _logger.Log("WARN", $"Спроба додати ачивку до гри id={gameId} без авторизації");
                return Redirect("/admin/login");
            }

            try
            {
                var game = _context.Games.Find(gameId);
                if (game == null)
                {
                    _logger.Log("WARN", $"Спроба додати ачивку до неіснуючої гри id={gameId}");
                    return NotFound();
                }

                var achievement = new Achievement
                {
                    GameId = gameId,
                    Title = title.Trim(),
                    Description = description.Trim(),
                    ImageUrl = string.IsNullOrWhiteSpace(ImageUrl) ? null : ImageUrl.Trim()
                };

                _context.Achievements.Add(achievement);
                _context.SaveChanges();

                _logger.Log("INFO", $"Додано ачивку: id={achievement.Id}, gameId={gameId}, title={achievement.Title}");
                return Redirect($"/admin/details/{gameId}");
            }
            catch (Exception ex)
            {
                _logger.Log("ERROR", $"Помилка при додаванні ачивки до гри id={gameId}: {ex.Message}");
                return Content($"<h1>Помилка при додаванні ачивки</h1><p>{Encode(ex.Message)}</p>", "text/html; charset=utf-8");
            }
        }

        [HttpGet("achievement/delete/{id}")]
        public IActionResult DeleteAchievement(int id)
        {
            if (!IsAuthorized())
            {
                _logger.Log("WARN", $"Спроба видалити ачивку id={id} без авторизації");
                return Redirect("/admin/login");
            }

            try
            {
                var achievement = _context.Achievements.Find(id);
                if (achievement == null)
                {
                    _logger.Log("WARN", $"Спроба видалити неіснуючу ачивку id={id}");
                    return Redirect("/admin");
                }

                var gameId = achievement.GameId;

                var userAchievements = _context.UserAchievements
                    .Where(ua => ua.AchievementId == id)
                    .ToList();

                _context.UserAchievements.RemoveRange(userAchievements);
                _context.Achievements.Remove(achievement);
                _context.SaveChanges();

                _logger.Log("INFO", $"Видалено ачивку: id={id}, gameId={gameId}");
                return Redirect($"/admin/details/{gameId}");
            }
            catch (Exception ex)
            {
                _logger.Log("ERROR", $"Помилка при видаленні ачивки id={id}: {ex.Message}");
                return Content($"<h1>Помилка при видаленні ачивки</h1><p>{Encode(ex.Message)}</p>", "text/html; charset=utf-8");
            }
        }

        [HttpGet("delete/{id}")]
        public IActionResult DeleteGame(int id)
        {
            if (!IsAuthorized())
            {
                _logger.Log("WARN", $"Спроба видалити гру id={id} без авторизації");
                return Redirect("/admin/login");
            }

            try
            {
                var game = _context.Games.Find(id);
                if (game == null)
                {
                    _logger.Log("WARN", $"Спроба видалити неіснуючу гру id={id}");
                    return Redirect("/admin");
                }

                var screenshots = _context.GameImages.Where(i => i.GameId == id).ToList();
                var genres = _context.GameGenres.Where(gg => gg.GameId == id).ToList();
                var achievements = _context.Achievements.Where(a => a.GameId == id).ToList();
                var achievementIds = achievements.Select(a => a.Id).ToList();
                var userAchievements = _context.UserAchievements
                    .Where(ua => achievementIds.Contains(ua.AchievementId))
                    .ToList();

                _context.UserAchievements.RemoveRange(userAchievements);
                _context.Achievements.RemoveRange(achievements);
                _context.GameImages.RemoveRange(screenshots);
                _context.GameGenres.RemoveRange(genres);
                _context.Games.Remove(game);

                _context.SaveChanges();

                _logger.Log("WARN", $"Видалено гру: id={id}, title={game.Title}");
                return Redirect("/admin");
            }
            catch (Exception ex)
            {
                _logger.Log("ERROR", $"Помилка при видаленні гри id={id}: {ex.Message}");
                return Content($"<h1>Помилка при видаленні гри</h1><p>{Encode(ex.Message)}</p>", "text/html; charset=utf-8");
            }
        }

        [HttpGet("edit/{id}")]
        public IActionResult EditGameForm(int id)
        {
            if (!IsAuthorized())
            {
                _logger.Log("WARN", $"Спроба відкрити редагування гри id={id} без авторизації");
                return Redirect("/admin/login");
            }

            var game = _context.Games.Find(id);
            if (game == null)
            {
                _logger.Log("WARN", $"Спроба відкрити редагування неіснуючої гри id={id}");
                return NotFound();
            }

            _logger.Log("INFO", $"Відкрито форму редагування гри id={id}, title={game.Title}");

            var genres = _context.Genres.AsNoTracking().ToList();
            var selectedGenreIds = _context.GameGenres
                .Where(gg => gg.GameId == id)
                .Select(gg => gg.GenreId)
                .ToHashSet();

            var genreOptions = BuildGenreOptions(genres, selectedGenreIds);

            var screenshots = _context.GameImages
                .Where(i => i.GameId == id)
                .Select(i => i.ImageUrl)
                .ToList();

            var screenshotPreview = new StringBuilder();
            foreach (var url in screenshots)
            {
                screenshotPreview.Append($@"<img src='{Encode(url)}' class='screenshot-small' />");
            }

            var screenshotUrlInputs = new StringBuilder();
            if (screenshots.Any())
            {
                foreach (var url in screenshots)
                {
                    screenshotUrlInputs.Append($@"
<div class='extra-file'>
    <input type='text' name='screenshotUrls' value='{Encode(url)}' placeholder='https://...' />
    <button type='button' class='btn small danger' onclick='removeScreenshotField(this)'>Видалити</button>
</div>");
                }
            }
            else
            {
                screenshotUrlInputs.Append(@"
<div class='extra-file'>
    <input type='text' name='screenshotUrls' placeholder='https://...' />
</div>");
            }

            var html = $@"
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Редагувати гру</title>
    <link rel='stylesheet' href='/css/admin.css?v=7'>
    <script>
        function addScreenshotUrl() {{
            const container = document.getElementById('screenshots');
            const wrapper = document.createElement('div');
            wrapper.className = 'extra-file';

            wrapper.innerHTML = `
                <input type='text' name='screenshotUrls' placeholder='https://...' />
                <button type='button' class='btn small danger' onclick='removeScreenshotField(this)'>Видалити</button>
            `;

            container.appendChild(wrapper);
        }}

        function removeScreenshotField(button) {{
            button.parentElement.remove();
        }}
    </script>
</head>
<body>
    <div class='admin-page'>
        <div class='background-blur blur-1'></div>
        <div class='background-blur blur-2'></div>

        <div class='edit-page glass-panel'>
            <div class='modal-header'>
                <div>
                    <div class='panel-badge'>РЕДАГУВАННЯ ГРИ</div>
                    <h2>Редагувати гру: {Encode(game.Title)}</h2>
                </div>
                <a href='/admin' class='btn secondary'>Назад</a>
            </div>

            <form method='post' action='/admin/edit/{game.Id}' class='admin-form'>
                <div class='form-grid'>
                    <div class='form-group'>
                        <label>Назва гри</label>
                        <input type='text' name='title' value='{Encode(game.Title)}' placeholder='Назва гри' required />
                    </div>

                    <div class='form-group'>
                        <label>Ціна (₴)</label>
                        <input type='number' step='0.01' name='price' value='{game.Price}' placeholder='Ціна (₴)' required />
                    </div>

                    <div class='form-group'>
                        <label>Дата виходу</label>
                        <input type='date' name='releaseDate' value='{game.ReleaseDate:yyyy-MM-dd}' required />
                    </div>

                    <div class='form-group full'>
                        <label>Опис</label>
                        <textarea name='description' placeholder='Опис гри' required>{Encode(game.Description)}</textarea>
                    </div>

                    <div class='form-group'>
                        <label>Знижка (%)</label>
                        <input type='number' name='discountPercent' value='{game.DiscountPercent}' placeholder='Знижка (%)' />
                    </div>

                    <div class='form-group'>
                        <label>Розробник</label>
                        <input type='text' name='developer' value='{Encode(game.Developer)}' placeholder='Розробник' required />
                    </div>

                    <div class='form-group'>
                        <label>Видавець</label>
                        <input type='text' name='publisher' value='{Encode(game.Publisher)}' placeholder='Видавець' required />
                    </div>

                    <div class='form-group full'>
                        <label>URL обкладинки</label>
                        <input type='text' name='coverUrl' value='{Encode(game.CoverImageUrl)}' placeholder='https://...' />
                    </div>

                    <div class='form-group full'>
                        <label>Поточні скріншоти</label>
                        <div class='screenshots'>
                            {screenshotPreview}
                        </div>
                    </div>

                    <div class='form-group full'>
                        <label>Скріншоти (URL)</label>
                        <div id='screenshots' class='screenshots-upload'>
                            {screenshotUrlInputs}
                        </div>
                        <button type='button' class='btn small secondary' onclick='addScreenshotUrl()'>Додати ще URL</button>
                    </div>

                    <div class='form-group full'>
                        <label>Жанри</label>
                        <select name='genreIds' multiple class='multi-select' required>
                            {genreOptions}
                        </select>
                    </div>
                </div>

                <div class='form-actions'>
                    <button type='submit' class='btn primary'>Зберегти</button>
                    <a href='/admin' class='btn secondary'>Скасувати</a>
                </div>
            </form>
        </div>
    </div>
</body>
</html>";

            return Content(html, "text/html; charset=utf-8");
        }

        [HttpPost("edit/{id}")]
        public IActionResult EditGame(
            int id,
            [FromForm] string title,
            [FromForm] string description,
            [FromForm] decimal price,
            [FromForm] DateTime releaseDate,
            [FromForm] int? discountPercent,
            [FromForm] string developer,
            [FromForm] string publisher,
            [FromForm] int[] genreIds,
            [FromForm] string? coverUrl,
            [FromForm] List<string>? screenshotUrls)
        {
            if (!IsAuthorized())
            {
                _logger.Log("WARN", $"Спроба оновити гру id={id} без авторизації");
                return Redirect("/admin/login");
            }

            try
            {
                var game = _context.Games.Find(id);
                if (game == null)
                {
                    _logger.Log("WARN", $"Спроба оновити неіснуючу гру id={id}");
                    return NotFound();
                }

                game.Title = title;
                game.Description = description;
                game.Price = price;
                game.ReleaseDate = DateTime.SpecifyKind(releaseDate, DateTimeKind.Utc);
                game.DiscountPercent = discountPercent;
                game.Developer = developer;
                game.Publisher = publisher;
                game.CoverImageUrl = !string.IsNullOrWhiteSpace(coverUrl)
                    ? coverUrl.Trim()
                    : "/images/default_cover.png";

                var oldGenres = _context.GameGenres.Where(gg => gg.GameId == id).ToList();
                _context.GameGenres.RemoveRange(oldGenres);

                foreach (var genreId in genreIds)
                {
                    _context.GameGenres.Add(new GameGenre
                    {
                        GameId = id,
                        GenreId = genreId
                    });
                }

                var oldScreenshots = _context.GameImages.Where(i => i.GameId == id).ToList();
                _context.GameImages.RemoveRange(oldScreenshots);

                if (screenshotUrls != null)
                {
                    foreach (var url in screenshotUrls)
                    {
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            _context.GameImages.Add(new GameImage
                            {
                                GameId = id,
                                ImageUrl = url.Trim()
                            });
                        }
                    }
                }

                _context.SaveChanges();

                _logger.Log("INFO", $"Оновлено гру: id={id}, title={title}");
                return Redirect("/admin");
            }
            catch (Exception ex)
            {
                _logger.Log("ERROR", $"Помилка при оновленні гри id={id}: {ex.Message}");
                return Content($"<h1>Помилка при оновленні гри</h1><p>{Encode(ex.Message)}</p>", "text/html; charset=utf-8");
            }
        }

        [HttpGet("details/{id}")]
        public IActionResult Details(int id)
        {
            if (!IsAuthorized())
            {
                _logger.Log("WARN", $"Спроба відкрити інформацію про гру id={id} без авторизації");
                return Redirect("/admin/login");
            }

            var game = _context.Games.Find(id);
            if (game == null)
            {
                _logger.Log("WARN", $"Спроба відкрити інформацію про неіснуючу гру id={id}");
                return NotFound();
            }

            _logger.Log("INFO", $"Відкрито інформацію про гру id={id}, title={game.Title}");

            var screenshots = _context.GameImages
                .Where(i => i.GameId == id)
                .Select(i => i.ImageUrl)
                .ToList();

            var screenshotsHtml = new StringBuilder();
            if (screenshots.Any())
            {
                foreach (var url in screenshots)
                {
                    screenshotsHtml.Append($"<img src='{Encode(url)}' class='details-shot' />");
                }
            }
            else
            {
                screenshotsHtml.Append("<p class='empty-state'>Скріншоти відсутні.</p>");
            }

            var achievements = _context.Achievements
                .AsNoTracking()
                .Where(a => a.GameId == id)
                .OrderBy(a => a.Id)
                .ToList();

            var achievementsHtml = new StringBuilder();
            if (achievements.Any())
            {
                foreach (var achievement in achievements)
                {
                    achievementsHtml.Append($@"
<div class='achievement-card'>
    <div class='achievement-icon-wrap'>
        <img src='{Encode(achievement.ImageUrl)}' class='achievement-icon' alt='{Encode(achievement.Title)}' />
    </div>

    <div class='achievement-content'>
        <h3>{Encode(achievement.Title)}</h3>
        <p>{Encode(achievement.Description)}</p>
    </div>

    <div class='achievement-actions'>
        <a class='btn small danger'
           href='/admin/achievement/delete/{achievement.Id}'
           onclick='return confirm(""Ви дійсно хочете видалити?"")'>
           Видалити
        </a>
    </div>
</div>");
                }
            }
            else
            {
                achievementsHtml.Append(@"
<div class='empty-achievements glass-panel'>
    <h3> Досягнень поки що немає </h3>
    <p>Для цієї гри поки не додано жодної ачивки.</p>
</div>");
            }

            var html = $@"
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>{Encode(game.Title)}</title>
    <link rel='stylesheet' href='/css/admin.css?v=7'>
    <script>
        function openAchievementModal() {{
            document.getElementById('achievementModal').classList.add('show');
            document.body.classList.add('modal-open');
        }}

        function closeAchievementModal() {{
            document.getElementById('achievementModal').classList.remove('show');
            document.body.classList.remove('modal-open');
        }}

        window.addEventListener('click', function(event) {{
            const modal = document.getElementById('achievementModal');
            if (event.target === modal) {{
                closeAchievementModal();
            }}
        }});
    </script>
</head>
<body>
    <div class='admin-page'>
        <div class='background-blur blur-1'></div>
        <div class='background-blur blur-2'></div>

        <div class='details-page glass-panel'>
            <a href='/admin' class='btn secondary back-btn'>← Назад в адмінку</a>

            <div class='details-layout'>
                <div class='details-cover-wrap'>
                    <img src='{Encode(game.CoverImageUrl)}' class='details-cover' />
                </div>

                <div class='details-info'>
                    <div class='panel-badge'>ІНФОРМАЦІЯ ПРО ГРУ</div>
                    <div class='title-row'>
                        <h1>{Encode(game.Title)}</h1>
                        <div class='idstyle'>ID: {game.Id}</div>
                    </div>
                    <p class='details-description'>{Encode(game.Description)}</p>

                    <div class='details-meta'>
                        <p><span>Ціна:</span> {game.Price} ₴</p>
                        <p><span>Знижка:</span> {(game.DiscountPercent.HasValue ? game.DiscountPercent + "%" : "Немає")}</p>
                        <p><span>Розробник:</span> {Encode(game.Developer)}</p>
                        <p><span>Видавець:</span> {Encode(game.Publisher)}</p>
                        <p><span>Дата виходу:</span> {game.ReleaseDate:dd.MM.yyyy}</p>
                    </div>

                    <div class='game-actions details-actions'>
                        <a class='btn small secondary' href='/admin/edit/{game.Id}'>Редагувати</a>
                        <button type='button' class='btn small primary' onclick='openAchievementModal()'>Додати досягнення</button>
                        <a class='btn small danger'
                           href='/admin/delete/{game.Id}'
                           onclick='return confirm(""Ви дійсно хочете видалити цю гру?"")'>
                           Видалити
                        </a>
                    </div>
                </div>
            </div>

            <div class='details-gallery'>
                <div class='section-header'>
                    <h2>Скріншоти</h2>
                    <span class='section-count'>{screenshots.Count}</span>
                </div>

                <div class='details-shots'>
                    {screenshotsHtml}
                </div>
            </div>

            <div class='details-achievements'>
                <div class='section-header'>
                    <h2>Досягнення</h2>
                    <span class='section-count'>{achievements.Count}</span>
                </div>

                <div class='achievements-list'>
                    {achievementsHtml}
                </div>
            </div>
        </div>
    </div>

    <div id='achievementModal' class='modal'>
        <div class='modal-content glass-panel'>
            <div class='modal-header'>
                <div>
                    <div class='panel-badge'>Досягнення</div>
                    <h2>Додати ачивку для гри: {Encode(game.Title)}</h2>
                </div>
                <button class='icon-close' type='button' onclick='closeAchievementModal()'>×</button>
            </div>

            <form method='post' action='/admin/achievement/add' class='admin-form'>
                <input type='hidden' name='gameId' value='{game.Id}' />

                <div class='form-grid'>
                    <div class='form-group'>
                        <label>Назва</label>
                        <input type='text' name='title' placeholder='Наприклад, Перший крок' required />
                    </div>

                    <div class='form-group full'>
                        <label>Опис</label>
                        <textarea name='description' placeholder='Опис досягнення' required></textarea>
                    </div>
<div class='form-group full'>
    <label>Іконка</label>
    <input type='text' name='ImageUrl' placeholder='https://...' />
</div>
                </div>

                <div class='form-actions'>
                    <button type='submit' class='btn primary'>Зберегти</button>
                    <button type='button' class='btn secondary' onclick='closeAchievementModal()'>Скасувати</button>
                </div>
            </form>
        </div>
    </div>
</body>
</html>";

            return Content(html, "text/html; charset=utf-8");
        }

        private bool IsValidAdminCredentials(string? user, string? pass)
        {
            return string.Equals(user?.Trim(), AdminUser, StringComparison.OrdinalIgnoreCase)
                && string.Equals(pass, AdminPass, StringComparison.Ordinal);
        }

        private bool IsAuthorized()
        {
            return Request.Cookies[AdminCookieName] == "true";
        }

        private string BuildLoginPage(string? errorMessage = null)
        {
            var errorHtml = string.IsNullOrWhiteSpace(errorMessage)
                ? ""
                : $"<div class='error-message'>{Encode(errorMessage)}</div>";

            return $@"
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Вхід адміністратора</title>
    <link rel='stylesheet' href='/css/admin.css?v=7'>
</head>
<body>
    <div class='admin-page'>
        <div class='background-blur blur-1'></div>
        <div class='background-blur blur-2'></div>

        <div class='login-card glass-panel'>
            <div class='panel-badge'>ДОСТУП АДМІНІСТРАТОРА</div>
            <h1>Вхід для адміністратора</h1>
            <p class='panel-subtitle'>Увійдіть, щоб керувати іграми та контентом сайту</p>

            {errorHtml}

            <form method='post' action='/admin/login' class='admin-form login-form'>
                <div class='form-group'>
                    <label>Користувач</label>
                    <input type='text' name='user' placeholder='Введіть логін' required />
                </div>

                <div class='form-group'>
                    <label>Пароль</label>
                    <input type='password' name='pass' placeholder='Введіть пароль' required />
                </div>

                <div class='form-actions'>
                    <button type='submit' class='btn primary'>Увійти</button>
                    <a href='/' class='btn secondary'>На головну</a>
                </div>
            </form>
        </div>
    </div>
</body>
</html>";
        }

        private string BuildGenreOptions(List<Genre> genres, HashSet<int>? selectedGenreIds = null)
        {
            var genreOptions = new StringBuilder();

            foreach (var g in genres)
            {
                var selected = selectedGenreIds != null && selectedGenreIds.Contains(g.Id) ? "selected" : "";
                var ukrName = genreTranslation.ContainsKey(g.Name) ? genreTranslation[g.Name] : g.Name;
                genreOptions.Append($"<option value='{g.Id}' {selected}>{Encode(ukrName)}</option>");
            }

            return genreOptions.ToString();
        }

        private string BuildGamesGridView(List<Game> games)
        {
            var gamesHtml = new StringBuilder();

            foreach (var game in games)
            {
                var coverUrl = !string.IsNullOrWhiteSpace(game.CoverImageUrl)
                    ? game.CoverImageUrl
                    : "/images/default_cover.png";

                gamesHtml.Append($@"
<a class='cover-tile' href='/admin/details/{game.Id}'>
    <div class='cover-tile-image-wrap'>
        <img src='{Encode(coverUrl)}' class='cover-tile-image' alt='{Encode(game.Title)}' />
    </div>
    <div class='cover-tile-overlay'>
        <div class='cover-tile-title'>{Encode(game.Title)}</div>
    </div>
</a>");
            }

            return gamesHtml.ToString();
        }

        private string BuildGamesCards(List<Game> games, List<GameImage> gameImages)
        {
            var gamesHtml = new StringBuilder();

            foreach (var game in games)
            {
                var coverUrl = !string.IsNullOrWhiteSpace(game.CoverImageUrl)
                    ? game.CoverImageUrl
                    : "/images/default_cover.png";

                var screenshotsHtml = string.Join("",
                    gameImages
                        .Where(img => img.GameId == game.Id)
                        .Select(img => $"<img src='{Encode(img.ImageUrl)}' class='screenshot-small' />"));

                var discountHtml = game.DiscountPercent != null
                    ? $"<span class='discount'>-{game.DiscountPercent}%</span>"
                    : "";

                gamesHtml.Append($@"
<div class='game-card glass-panel'>
    <div class='game-cover-wrap'>
        <img src='{Encode(coverUrl)}' class='game-cover' />
    </div>

    <div class='game-info'>
        <div class='game-header'>
            <h3>{Encode(game.Title)}</h3>
            <span class='game-price'>{game.Price} ₴ {discountHtml}</span>
        </div>

        <div class='game-meta'>
            <p><span>Розробник:</span> {Encode(game.Developer)}</p>
            <p><span>Видавець:</span> {Encode(game.Publisher)}</p>
            <p><span>Дата виходу:</span> {game.ReleaseDate:dd.MM.yyyy}</p>
        </div>

        <div class='game-actions'>
            <a class='btn small info' href='/admin/details/{game.Id}'>Інформація</a>
            <a class='btn small secondary' href='/admin/edit/{game.Id}'>Редагувати</a>
            <a class='btn small danger' href='/admin/delete/{game.Id}' onclick='return confirm(""Ви дійсно хочете видалити цю гру?"")'>Видалити</a>
        </div>

        <div class='screenshots'>
            {screenshotsHtml}
        </div>
    </div>
</div>");
            }

            return gamesHtml.ToString();
        }

        private static string Encode(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }
    }
}