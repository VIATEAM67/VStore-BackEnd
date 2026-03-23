using Microsoft.AspNetCore.Mvc;

namespace Server.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            _logger.LogInformation("Открыта главная страница в {Time}", DateTime.Now);

            var html = @"
            <html>
            <head>
                <meta charset='utf-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Статус сервера</title>
                <link rel='stylesheet' href='/css/home.css'>
            </head>
            <body>
                <div class='background-blur blur-1'></div>
                <div class='background-blur blur-2'></div>
                <div class='background-blur blur-2'></div>

                <div class='container'>
                    <div class='glass-card'>
                        <span class='badge'>Статус Сервера</span>
                        <h1>Сервер працює!</h1>
                        <p class='status'>Статус: <strong>OK</strong></p>
                        <p class='time'>Час: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + @"</p>

                        <div class='actions'>
                            <a class='btn primary' href='/admin'>Адмін Панель</a>
                            <a class='btn secondary' href='/logs'>Логирование</a>
                        </div>
                    </div>
                </div>
            </body>
            </html>";

            return Content(html, "text/html");
        }
    }
}