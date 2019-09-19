﻿using System;

namespace XCentium.Sitecore.XConnect.Console
{
    public class ConsoleLogger : ILogger
    {
        public void LogError(Exception ex)
        {
            System.Console.WriteLine($"ERROR: {ex.Message}");
        }

        public void LogInfo(string message)
        {
            System.Console.WriteLine($"INFO: {message}");
        }
    }
}
