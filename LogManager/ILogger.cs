namespace LogManager
{
    public interface ILogger
    {
        void LogMessage(string message);

        void LogException(Exception exception);
    }
}
