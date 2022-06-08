using System.Reflection;
using Duthie.Bot.Configuration;
using Duthie.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Duthie.Bot;

internal class DuthieDbContextFactory : IDesignTimeDbContextFactory<DuthieDbContext>
{
    public DuthieDbContextFactory()
    {
        ModuleLoader.LoadModules();
    }

    public DuthieDbContext CreateDbContext(string[] args)
    {
        var config = GetDatabaseConfiguration();
        var optionsBuilder = new DbContextOptionsBuilder<DuthieDbContext>();
        optionsBuilder.UseSqlite(config.ConnectionString, b => b.MigrationsAssembly("Duthie.Bot"));
        return new DuthieDbContext(optionsBuilder.Options);
    }

    private static IConfiguration GetConfiguration()
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

        return new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();
    }

    private static DatabaseConfiguration GetDatabaseConfiguration()
    {
        var databaseConfiguration = new DatabaseConfiguration();
        GetConfiguration().GetSection("Database").Bind(databaseConfiguration);
        return databaseConfiguration;
    }
}