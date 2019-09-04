using System;

namespace Sitecore.XConnect.Streaming
{
    public class CheckpointTracker
    {
        public void CreateCheckpoint(DateTime lastProcessedTimestamp)
        {
            // TODO: must implement this
            //throw new NotImplementedException();
        }

        public DateTime GetLastCheckpoint()
        {
            return new DateTime(2018, 08, 30).ToUniversalTime();
            //throw new NotImplementedException();
        }
    }
}
