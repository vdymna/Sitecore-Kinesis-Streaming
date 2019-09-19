using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;


namespace Sitecore.XConnect.Streaming.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var configBuilder = new ConfigurationBuilder();
            configBuilder.AddXmlFile("settings.xml", true);
            var config = configBuilder.Build();

            var streamingApp = new DataStreamingApp(config);
            Task.Run(() => streamingApp.RunAsync()).Wait();
        }
    }
}
