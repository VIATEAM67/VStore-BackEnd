using System.Text;

namespace Server.Services
{
    public class FileLogger
    {
        private readonly string _logFilePath;
        private readonly object _lock = new();

        public FileLogger(IWebHostEnvironment env)
        {
            var logsFolder = Path.Combine(env.ContentRootPath, "Logs");
            Directory.CreateDirectory(logsFolder);

            _logFilePath = Path.Combine(logsFolder, "server.log");
        }

        public void Log(string level, string message)
        {
            var kyivTimeZone = GetUkraineTimeZone();
            var localTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, kyivTimeZone);

            var line = $"[{localTime:dd.MM.yyyy HH:mm:ss}] [{level.ToUpper()}] {message}";

            lock (_lock)
            {
                File.AppendAllText(_logFilePath, line + Environment.NewLine, Encoding.UTF8);
            }
        }

        public List<string> ReadLatestLogs(int count = 120)
        {
            if (!File.Exists(_logFilePath))
                return new List<string>();

            var lines = File.ReadAllLines(_logFilePath, Encoding.UTF8);
            return lines.TakeLast(count).ToList();
        }

        private TimeZoneInfo GetUkraineTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("FLE Standard Time");
            }
            catch
            {
                return TimeZoneInfo.Local;
            }
        }
    }
}