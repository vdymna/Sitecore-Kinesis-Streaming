using System;

namespace Sitecore.DataStreaming.Utilities
{
    public interface ILogger
    {
        void LogError(Exception ex);

        void LogInfo(string message);
    }
}
