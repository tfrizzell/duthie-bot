// https://discordapp.com/oauth2/authorize?&client_id=435582099714605057&scope=bot&permissions=2048

using System.Reflection;
using Discord;
using Discord.WebSocket;
using Duthie.Bot;
using Duthie.Bot.Configuration;
using Duthie.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

await LoadModules();

var services = new ServiceCollection()
    .ConfigureServices()
    .AddDiscord()
    .AddApi()
    .AddAppInfo();

await MainAsync();

Task LoadModules()
{
    var modules = new List<string> { Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory) ?? ".", "Duthie.Modules.dll") };
    var moduleDir = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory) ?? ".", "Modules");

    if (Directory.Exists(moduleDir))
        modules.AddRange(Directory.EnumerateFiles(moduleDir, "*.dll"));

    foreach (var module in modules)
        Assembly.LoadFile(module);

    return Task.CompletedTask;
}

async Task MainAsync()
{
    var serviceProvider = services.BuildServiceProvider();

    foreach (var handler in serviceProvider.GetRequiredService<IEnumerable<IAsyncHandler>>())
        await handler.RunAsync();

    var discordConfig = serviceProvider.GetRequiredService<DiscordConfiguration>();

    if (string.IsNullOrWhiteSpace(discordConfig.Token))
    {
        throw new ArgumentException($"Configuration option Discord:Token cannot be empty");
    }

    using (var context = serviceProvider.GetRequiredService<DuthieDbContext>())
    {
        context.Database.Migrate();
        await context.PopulateAsync();
    }

    var client = serviceProvider.GetRequiredService<DiscordShardedClient>();
    await client.LoginAsync(TokenType.Bot, discordConfig.Token);
    await client.StartAsync();
    await Task.Delay(Timeout.Infinite);
}