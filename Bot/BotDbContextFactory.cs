using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using MySQL.Data.EntityFrameworkCore.Extensions;

namespace Bot
{
    internal class BotDbContextFactory : IDbContextFactory<BotDbContext> {

        private readonly string _connection;

        public BotDbContextFactory()
        {
            var enviroment = (string)Environment.GetEnvironmentVariables()["enviroment"] ?? "default";
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(
                    Path.Combine(
                        "Config", $"{enviroment}.config.json"
                    ), optional: false, reloadOnChange: true).Build();

            _connection = config.GetConnectionString("bot");
        }

        public BotDbContextFactory(string connection)
        {
            _connection = connection;
        }

        public BotDbContext Create(DbContextFactoryOptions options)
        {
            var optionsBuilder = new DbContextOptionsBuilder<BotDbContext>();
            optionsBuilder.UseMySQL(_connection);

            //Ensure database creation
            var context = new BotDbContext(optionsBuilder.Options);
            context.Database.Migrate();
            return context;
        }
    }
}
