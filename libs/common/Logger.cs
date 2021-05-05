namespace libs.common
{
    using System;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    public class Logger<T> : NullLogger<T>
    {
        public new void Log<TState> (LogLevel logLevel, 
                            EventId eventId, 
                            TState state, 
                            Exception exception, 
                            Func<TState,Exception,string> formatter)
        {
            Console.WriteLine("Log");
            base.Log(logLevel, eventId, state, exception, formatter);
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
    }
}