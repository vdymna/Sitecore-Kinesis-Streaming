using Microsoft.Extensions.Configuration;
using Sitecore.Streaming.Utilities;
using System;
using System.Threading.Tasks;


namespace Sitecore.Streaming
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddXmlFile("settings.xml", true);
            var config = configBuilder.Build();

            ILogger logger = new ConsoleLogger();

            using (var pipeline = new DataStreamingPipeline(config, logger))
            {
                pipeline.Initialize();

                var runTime = TimeSpan.FromMinutes(60);
                Task.Run(() => pipeline.RunAsync(runTime)).Wait();
            }
        }
    }
}
