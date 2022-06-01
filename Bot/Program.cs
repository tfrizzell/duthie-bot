// https://discordapp.com/oauth2/authorize?&client_id=435582099714605057&scope=bot&permissions=2048

using Discord;
using Discord.WebSocket;
using Duthie.Bot;
using Duthie.Bot.Configuration;
using Duthie.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .ConfigureServices()
    .AddDiscord()
    .AddApi()
    .AddAppInfo();

await MainAsync();

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
    }

    var client = serviceProvider.GetRequiredService<DiscordShardedClient>();
    await client.LoginAsync(TokenType.Bot, discordConfig.Token);
    await client.StartAsync();
    await Task.Delay(Timeout.Infinite);
}