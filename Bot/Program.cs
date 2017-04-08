using System.Diagnostics;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bot
{
    internal class Program
    {
        private static string Enviroment(string[] args)
        {
            var builder = new ConfigurationBuilder();
            var config = builder.AddEnvironmentVariables().AddCommandLine(args).Build();
            return config["enviroment"] ?? "dev";

        }

        private static IConfigurationBuilder BuildConfiguration(string[] args)
        {
            var enviroment = Enviroment(args);
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    Path.Combine(
                        "Data", $"{enviroment}.config.json"
                    ), optional: false, reloadOnChange: true)
                .AddEnvironmentVariables(enviroment)
                .AddCommandLine(args);
        }

        private static void Main(string[] args)
        {
            // Enviroment Information
            var exePath = Process.GetCurrentProcess().MainModule.FileName;            
            var directoryPath = Path.GetDirectoryName(exePath);
            var host = new WebHostBuilder();
          
            // Create the Configuration Keystore
            var builder = BuildConfiguration(args);

            // Build the Configuration Object
            var config = builder.Build();

            // Grab a Listen of Web URLS to Listen on
            var urls = config.GetSection("web:urls").Get<string[]>() ?? new []{"http://localhost:5050"};

            // Finish Building Host
            host
                .UseContentRoot(directoryPath)
                .UseKestrel()
                .UseStartup<Startup>()
                .UseUrls(urls)
                .UseConfiguration(config)
                .ConfigureServices( services => services.AddSingleton(config))
                .Build()
                .Run();

        }

    }
}