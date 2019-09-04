using System.Threading.Tasks;


namespace Sitecore.XConnect.Streaming.Console
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var streamingApp = new DataStreamingApp();
            Task.Run(() => streamingApp.RunAsync()).Wait();
        }
    }
}
