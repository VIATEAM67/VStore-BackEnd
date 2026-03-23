using Microsoft.AspNetCore.Mvc;
using Server.Services;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Server.Controllers
{
    [ApiController]
    [Route("logs")]
    public class LogsController : ControllerBase
    {
        private readonly FileLogger _fileLogger;

        public LogsController(FileLogger fileLogger)
        {
            _fileLogger = fileLogger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            var logs = _fileLogger.ReadLatestLogs(120)
                .AsEnumerable()
                .Reverse()
                .ToList();

            var logsHtml = new StringBuilder();

            if (logs.Count == 0)
            {
                logsHtml.Append("<div class='empty-state'>Логи пока отсутствуют.</div>");
            }
            else
            {
                foreach (var line in logs)
                {
                    var match = Regex.Match(line, @"^\[(.*?)\]\s\[(.*?)\]\s(.*)$");

                    string date = "";
                    string level = "INFO";
                    string message = line;

                    if (match.Success)
                    {
                        date = WebUtility.HtmlEncode(match.Groups[1].Value);
                        level = WebUtility.HtmlEncode(match.Groups[2].Value.ToUpper());
                        message = WebUtility.HtmlEncode(match.Groups[3].Value);
                    }
                    else
                    {
                        message = WebUtility.HtmlEncode(line);
                    }

                    var cssClass = level switch
                    {
                        "ERROR" => "error",
                        "WARN" => "warn",
                        "INFO" => "info",
                        _ => "default"
                    };

                    logsHtml.Append($@"
<div class='log-row {cssClass}'>
    <div class='log-date'>{date}</div>
    <div class='log-level'>{level}</div>
    <div class='log-message'>{message}</div>
</div>");
                }
            }

            var html = $@"
<!DOCTYPE html>
<html lang='ru'>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <meta http-equiv='refresh' content='5'>
    <title>Логи сервера</title>
    <link rel='stylesheet' href='/css/logs.css?v=3'>
</head>
<body>
    <div class='logs-page'>
        <div class='logs-shell'>
            <div class='logs-header'>
                <div>
                    <div class='panel-badge'>SERVER LOGS</div>
                    <h1>Логи сервера</h1>
                    <p class='panel-subtitle'>
                        Последние действия системы, запросы, предупреждения и ошибки
                    </p>
                </div>

                <div class='header-actions'>
                    <a class='btn secondary' href='/'>На главную</a>
                    <a class='btn primary' href='/logs'>Обновить</a>
                </div>
            </div>

            <div class='logs-panel'>
                <div class='logs-toolbar'>
                    <span class='logs-count'>Записей: {logs.Count}</span>
                    <span class='auto-refresh'>Автообновление: каждые 5 сек</span>
                </div>

                <div class='logs-container'>
                    {logsHtml}
                </div>
            </div>
        </div>
    </div>
</body>
</html>";

            return Content(html, "text/html; charset=utf-8");
        }
    }
}