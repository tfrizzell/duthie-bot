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
        var configuration = GetConfiguration(args);
        var databaseConfiguration = GetDatabaseConfiguration(configuration);
        databaseConfiguration.Type = configuration.GetValue<DatabaseType>("Provider", databaseConfiguration.Type);
        databaseConfiguration.ConnectionString = configuration.GetValue<string>("ConnectionString", databaseConfiguration.ConnectionString);

        var options = new DbContextOptionsBuilder<DuthieDbContext>();
        DuthieDbContextFactory.ConfigureOptions(options, databaseConfiguration);
        return new DuthieDbContext(options.Options);
    }

    private static IConfiguration GetConfiguration(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

        return new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .AddCommandLine(args)
            .Build();
    }

    private static DatabaseConfiguration GetDatabaseConfiguration(string[] args) =>
        GetDatabaseConfiguration(GetConfiguration(args));

    private static DatabaseConfiguration GetDatabaseConfiguration(IConfiguration configuration)
    {
        var databaseConfiguration = new DatabaseConfiguration();
        configuration.GetSection("Database").Bind(databaseConfiguration);
        return databaseConfiguration;
    }

    public static void ConfigureOptions(DbContextOptionsBuilder options, DatabaseConfiguration configuration)
    {
        switch (configuration.Type)
        {
            case DatabaseType.Sqlite:
                options.UseSqlite(configuration.ConnectionString, b => b.MigrationsAssembly("Duthie.Migrations.Sqlite"));
                break;

            case DatabaseType.Mysql:
                options.UseMySql(configuration.ConnectionString, ServerVersion.AutoDetect(configuration.ConnectionString), b => b.MigrationsAssembly("Duthie.Migrations.Mysql"));
                break;

            default:
                throw new ArgumentException($"Invalid database type {configuration.Type}");
        }
    }
}