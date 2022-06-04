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
        LoadModules();
    }

    public DuthieDbContext CreateDbContext(string[] args)
    {
        var config = GetDatabaseConfiguration();
        var optionsBuilder = new DbContextOptionsBuilder<DuthieDbContext>();

        switch (config.Type)
        {
            case DatabaseType.MySql:
                optionsBuilder.UseMySql(config.ConnectionString, MariaDbServerVersion.AutoDetect(config.ConnectionString), b => b.MigrationsAssembly("Duthie.Bot"));
                break;

            case DatabaseType.Sqlite:
                optionsBuilder.UseSqlite(config.ConnectionString, b => b.MigrationsAssembly("Duthie.Bot"));
                break;
        }

        return new DuthieDbContext(optionsBuilder.Options);
    }

    private static void LoadModules()
    {
        var modules = new List<string> { };
        var moduleDir = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory) ?? ".", "modules");

        if (Directory.Exists(moduleDir))
            modules.AddRange(Directory.EnumerateFiles(moduleDir, "*.dll"));

        foreach (var module in modules)
            Assembly.LoadFile(module);
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