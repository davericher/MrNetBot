using System;
using System.IO;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MySQL.Data.EntityFrameworkCore.Extensions;

namespace Bot
{
    internal class Program
    {
        // Get the Current enviroment or a sensinble default
        private static string CurrentEnviroment(string[] args) => (string)Environment.GetEnvironmentVariables()["enviroment"] ?? "default";
        

        private static IConfigurationBuilder BuildConfiguration(string[] args)
        {
            var enviroment = CurrentEnviroment(args);
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    Path.Combine(
                        "Config", $"{enviroment}.config.json"
                    ), optional: false, reloadOnChange: true)
                .AddEnvironmentVariables(enviroment)
                .AddCommandLine(args);
        }

        private static void Main(string[] args)
        {
            // CurrentEnviroment Information
            var exePath = Process.GetCurrentProcess().MainModule.FileName;            
            var directoryPath = Path.GetDirectoryName(exePath);
            var host = new WebHostBuilder();
          
            // Create the Configuration Keystore
            var builder = BuildConfiguration(args);

            // Build the Configuration Object
            var config = builder.Build();

            // DB Connection 
            var sqlConnectionString = config.GetConnectionString("bot");

            // Grab a Listen of Web URLS to Listen on
            var urls = config.GetSection("web:urls").Get<string[]>() ?? new []{"http://localhost:5050"};

            try
            {
                // Finish Building Host
                host
                    .UseContentRoot(directoryPath)
                    .UseKestrel()
                    .UseStartup<Startup>()
                    .UseUrls(urls)
                    .UseConfiguration(config)
                    .ConfigureServices(services => 
                        services
                        .AddSingleton(config)
                        .AddDbContext<BotDbContext>(optionsBuilder => optionsBuilder.UseMySQL(sqlConnectionString) )
                    )
                    .Build()
                    .Run();
            }
            catch (IOException)
            {
                Console.WriteLine("Was not able to bind to any HTTP Hosts, terminated...");
                System.Environment.Exit(1);
            }
        }

    }
}