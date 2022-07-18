using SiteCrawler.Services;
using System;
using System.Threading.Tasks;

namespace SiteCrawler
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            CrawlerManagerService _service = new("http://www.22bugs.co/");
            await _service.Run();
        }
    }
}
