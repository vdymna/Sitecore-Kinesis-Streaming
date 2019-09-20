using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;


namespace Sitecore.DataStreaming.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddXmlFile("settings.xml", true);
            var config = configBuilder.Build();

            var pipeline = new DataStreamingPipeline(config);
            Task.Run(() => pipeline.RunAsync()).Wait();
        }
    }
}
