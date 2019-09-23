using System;
using static System.Console;

namespace Sitecore.DataStreaming.Utilities
{
    public class ConsoleLogger : ILogger
    {
        public void LogError(Exception ex)
        {
            WriteLine($"ERROR: {ex.GetType().Name} - {ex.Message}");

            var inner = ex.InnerException;
            while (inner != null)
            {
                WriteLine($"ERROR: {inner.GetType().Name} - {inner.Message}{Environment.NewLine}{ex.StackTrace}");
                inner = inner.InnerException;
            }
        }

        public void LogInfo(string message)
        {
            WriteLine($"INFO: {message}");
        }
    }
}
