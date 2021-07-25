namespace libs.common
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    public class Logger<T> : ILogger<T>, ILogger
    {
        public void Log<TState> (LogLevel logLevel, 
                            EventId eventId, 
                            TState state, 
                            Exception exception, 
                            Func<TState,Exception,string> formatter)
        {
            var message = $"{DateTime.UtcNow} - {logLevel}: ";
            if (formatter != null)
            {
                message = $"{message} {formatter(state, exception)}";
            }

            Console.WriteLine(message);
        }

        public void LogInformation(string message)
        {            
            Console.WriteLine($"{DateTime.UtcNow} - Information: {message}");           
        }

        public void LogWarning(string message)
        {            
            Console.WriteLine($"{DateTime.UtcNow} - Warning: {message}");             
        }

        public void LogError(string message)
        {
            Console.WriteLine($"{DateTime.UtcNow} - Error: {message}");            
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return (logLevel == LogLevel.Information || 
                    logLevel == LogLevel.Error || 
                    logLevel == LogLevel.Warning);
        }

        public IDisposable BeginScope<TState>(TState state)
        {
           var logger =  NullLoggerFactory.Instance.CreateLogger<T>();
           return logger.BeginScope<TState>(state);
        }
    }
}