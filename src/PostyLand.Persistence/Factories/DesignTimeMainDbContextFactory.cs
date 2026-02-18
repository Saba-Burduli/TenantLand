using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PostyLand.Persistence.Context;

namespace PostyLand.Persistence.Factories;

public sealed class DesignTimeMainDbContextFactory : IDesignTimeDbContextFactory<MainDbContext>
{
    public MainDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("POSTYLAND_MAIN_DB_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=postyland_main_dev;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.MigrationsAssembly(typeof(MainDbContext).Assembly.FullName))
            .Options;

        return new MainDbContext(options);
    }
}
