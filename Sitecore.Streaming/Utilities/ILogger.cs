using System;

namespace Sitecore.Streaming.Utilities
{
    public interface ILogger
    {
        void LogError(Exception ex);

        void LogInfo(string message);
    }
}
