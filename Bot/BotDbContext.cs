namespace Bot
{
    using Microsoft.EntityFrameworkCore;

    class BotDbContext : DbContext
    {
        public BotDbContext(DbContextOptions<BotDbContext> options) : base(options) {}
    }
}
