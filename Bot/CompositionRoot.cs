using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Duthie.Bot.Commands;
using Duthie.Bot.Configuration;
using Duthie.Bot.Events;
using Duthie.Bot.Services;
using Duthie.Data;
using Duthie.Modules.LeagueGaming;
using Duthie.Modules.MyVirtualGaming;
using Duthie.Modules.TheSpnhl;
using Duthie.Services.Background;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Sites;
using Duthie.Services.Teams;
using Duthie.Services.Watchers;
using Duthie.Types.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot;

public static class CompositionRoot
{
    public static IServiceCollection ConfigureServices(this IServiceCollection services)
    {
        var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production";

        var config = new ConfigurationBuilder()
            .AddJsonFile($"appsettings.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)
            .AddUserSecrets<Program>()
            .AddEnvironmentVariables()
            .Build();

        var databaseConfiguration = new DatabaseConfiguration();
        config.GetSection("Database").Bind(databaseConfiguration);

        services.AddDbContextFactory<DuthieDbContext>(options =>
        {
            switch (databaseConfiguration.Type)
            {
                case DatabaseType.MySql:
                    options.UseMySql(databaseConfiguration.ConnectionString, MariaDbServerVersion.AutoDetect(databaseConfiguration.ConnectionString), b => b.MigrationsAssembly("Duthie.Bot"));
                    break;

                case DatabaseType.Sqlite:
                    options.UseSqlite(databaseConfiguration.ConnectionString, b => b.MigrationsAssembly("Duthie.Bot"));
                    break;
            }
        });

        services.AddLogging(options =>
        {
            options
                .ClearProviders()
                .AddConfiguration(config.GetSection("Logging"))
                .AddSimpleConsole(builder =>
                {
                    builder.IncludeScopes = true;
                    builder.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                });
        });

        services.AddMemoryCache();

        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton(databaseConfiguration);

        services.AddSingleton<GuildService>();
        services.AddSingleton<GuildAdminService>();
        services.AddSingleton<SiteService>();
        services.AddSingleton<LeagueService>();
        services.AddSingleton<TeamService>();
        services.AddSingleton<WatcherService>();
        services.AddSingleton<GuildMessageService>();

        services.AddHandler<ProgramEventHandler>();
        return services;
    }

    public static IServiceCollection AddDiscord(this IServiceCollection services)
    {
        var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var discordConfig = new DiscordConfiguration();
        config.GetSection("Discord").Bind(discordConfig);

        var client = new DiscordShardedClient(new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.GuildMembers,
        });

        var commands = new CommandService(new CommandServiceConfig
        {
            LogLevel = config.GetValue<LogSeverity>("Logging:LogLevel:Discord", LogSeverity.Info),
            CaseSensitiveCommands = false,
        });

        services.AddSingleton(discordConfig);
        services.AddSingleton(client);
        services.AddSingleton(commands);
        services.AddSingleton<CommandRegistrationService>();

        services.AddHandler<GuildEventHandler>();
        services.AddHandler<CommandEventHandler>();
        services.AddHandler<DiscordEventHandler>();

        services.AddCommand<PingCommand>();
        services.AddCommand<AdminCommand>();
        services.AddCommand<WatchersCommand>();
        services.AddCommand<ListCommand>();
        return services;
    }

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        var apiService = new ApiService();
        services.AddSingleton(apiService);

        apiService.Register(new LeagueGamingApi());
        apiService.Register(new MyVirtualGamingApi());
        apiService.Register(new TheSpnhlApi());

        var apis = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(t => !t.IsAbstract
                && typeof(IApi).IsAssignableFrom(t)
                && typeof(LeagueGamingApi) != t
                && typeof(MyVirtualGamingApi) != t
                && typeof(TheSpnhlApi) != t);


        foreach (var api in apis)
            apiService.Register((IApi)Activator.CreateInstance(api)!);

        services.AddSingleton<LeagueUpdateService>();
        return services;
    }

    public static IServiceCollection AddAppInfo(this IServiceCollection services)
    {
        var assembly = Assembly.GetEntryAssembly() ?? new AppInfo().GetType().Assembly;

        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? assembly.GetName().Version?.ToString()
            ?? "";

        services.AddSingleton(new AppInfo(Version: version));
        return services;
    }

    private static void AddHandler<T>(this IServiceCollection services)
        where T : class, IAsyncHandler
    {
        services.AddSingleton<T>();
        services.AddSingleton<IAsyncHandler>(s => s.GetRequiredService<T>());
    }

    private static void AddCommand<T>(this IServiceCollection services)
        where T : class, ICommand
    {
        services.AddSingleton<T>();
        services.AddSingleton<ICommand>(s => s.GetRequiredService<T>());
    }
}