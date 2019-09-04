using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sitecore.XConnect.Streaming.Dtos
{
    public class PageViewInteractionDto
    {
        public Guid? ContactId { get; set; }

        public string EmailAddress { get; set; }

        public Guid? InteractionId { get; set; }

        public string SiteName { get; set; }

        public string UserAgent { get; set; }

        public string IpAddress { get; set; }

        //public string MetroCode { get; set; }

        //public string Country { get; set; }

        public Guid? PageViewEventId { get; set; }

        public DateTime Timestamp { get; set; }

        public TimeSpan Duration { get; set; }

        public string Url { get; set; }

    }
}
