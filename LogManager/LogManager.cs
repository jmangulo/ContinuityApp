namespace LogManager
{
    public class Logger : ILogger
    {
        private readonly string logFilePath;

        public Logger(string logFilePath)
        {
            this.logFilePath = logFilePath;
            InitializeLogDirectory(logFilePath);
        }

        public void LogMessage(string message)
        {
            WriteLog(LogType.Message, message);
        }

        public void LogException(Exception exception)
        {
            string message = exception.Message;
            string? stackTrace = exception.StackTrace;
            if (exception.InnerException != null)
            {
                message += "\n" + exception.InnerException.Message;
            }
            WriteLog(LogType.Exception, message);
            if (stackTrace != null)
            {
                WriteLog(LogType.Exception, stackTrace);
            }
        }

        private string ConstructMessage(LogType logType, string message)
        {
            return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logType}] {message}";
        }

        private void InitializeLogDirectory(string logFilePath)
        {
            string? logDirectory = Path.GetDirectoryName(logFilePath);
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }
            if (!File.Exists(logFilePath))
            {
                using FileStream fs = File.Create(logFilePath);
            }
        }

        private void WriteLog(LogType logType, string message)
        {
            string constructedMessage = ConstructMessage(logType, message);
            try
            {
                File.AppendAllText(logFilePath, constructedMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}