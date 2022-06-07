using System.Reflection;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Duthie.Bot.Commands;
using Duthie.Bot.Configuration;
using Duthie.Bot.Events;
using Duthie.Data;
using Duthie.Modules.LeagueGaming;
using Duthie.Modules.MyVirtualGaming;
using Duthie.Modules.TheSpnhl;
using Duthie.Services.Api;
using Duthie.Services.Games;
using Duthie.Services.Guilds;
using Duthie.Services.Leagues;
using Duthie.Services.Sites;
using Duthie.Services.Teams;
using Duthie.Services.Watchers;
using Duthie.Types.Modules.Api;
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

        return services.AddDbContextFactory<DuthieDbContext>(options =>
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
            })
            .AddLogging(options =>
            {
                options
                    .ClearProviders()
                    .AddConfiguration(config.GetSection("Logging"))
                    .AddSimpleConsole(builder =>
                    {
                        builder.IncludeScopes = true;
                        builder.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
                    });
            })
            .AddMemoryCache()
            .AddSingleton<IConfiguration>(config)
            .AddSingleton(databaseConfiguration)
            .AddSingleton<GuildService>()
            .AddSingleton<GuildAdminService>()
            .AddSingleton<SiteService>()
            .AddSingleton<LeagueService>()
            .AddSingleton<TeamService>()
            .AddSingleton<WatcherService>()
            .AddSingleton<GameService>()
            .AddSingleton<GuildMessageService>()
            .AddHandler<ProgramEventHandler>();
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

        return services
            .AddSingleton(discordConfig)
            .AddSingleton(client)
            .AddSingleton(commands)
            .AddSingleton<CommandRegistrationService>()
            .AddHandler<GuildEventHandler>()
            .AddHandler<CommandEventHandler>()
            .AddHandler<DiscordEventHandler>()
            .AddCommand<PingCommand>()
            .AddCommand<AdminCommand>()
            .AddCommand<WatcherCommand>()
            .AddCommand<ListCommand>();
    }

    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        var apiService = new ApiService();

        var apis = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(t => !t.IsAbstract
                && typeof(ISiteApi).IsAssignableFrom(t)
                && typeof(LeagueGamingApi) != t
                && typeof(MyVirtualGamingApi) != t
                && typeof(TheSpnhlApi) != t);

        apiService.Register(
            new List<ISiteApi>() {
                new LeagueGamingApi(),
                new MyVirtualGamingApi(),
                new TheSpnhlApi()
            }.Concat(apis.Select(api => (ISiteApi)Activator.CreateInstance(api)!)).ToArray());

        return services.AddSingleton(apiService);
    }

    public static IServiceCollection AddAppInfo(this IServiceCollection services)
    {
        var assembly = Assembly.GetEntryAssembly() ?? new AppInfo().GetType().Assembly;

        var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
            ?? assembly.GetName().Version?.ToString()
            ?? "";

        return services.AddSingleton(new AppInfo(Version: version));
    }

    private static IServiceCollection AddHandler<T>(this IServiceCollection services) where T : class, IAsyncHandler =>
        services.AddSingleton<T>()
            .AddSingleton<IAsyncHandler>(s => s.GetRequiredService<T>());

    private static IServiceCollection AddCommand<T>(this IServiceCollection services) where T : class, ICommand =>
        services.AddSingleton<T>()
            .AddSingleton<ICommand>(s => s.GetRequiredService<T>());
}