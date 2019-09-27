using System;

namespace Sitecore.Streaming.Dtos
{
    public class PageViewInteractionDto
    {
        public Guid PageViewEventId { get; set; }

        public string EventTimestamp { get; set; }

        public Guid? InteractionId { get; set; }

        public Guid? ContactId { get; set; }

        public string Url { get; set; }

        public int DurationInSeconds { get; set; }

        public string SiteName { get; set; }

        public string EmailAddress { get; set; }

        public string UserAgent { get; set; }

        public string IpAddress { get; set; }

    }
}
