using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bot
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var exePath = Process.GetCurrentProcess().MainModule.FileName;
            var directoryPath = Path.GetDirectoryName(exePath);

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(Path.Combine("Data", "config.json"), false, false)
                .AddEnvironmentVariables()
                .AddCommandLine(args);


            var host = new WebHostBuilder()
                .UseContentRoot(directoryPath)
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls("http://127.0.0.1:9090")
                .ConfigureServices( services => services.AddSingleton(builder.Build()))
                .Build();

            host.Run();
        }
    }
}