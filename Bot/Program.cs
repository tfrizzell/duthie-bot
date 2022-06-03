// https://discordapp.com/oauth2/authorize?&client_id=435582099714605057&scope=bot&permissions=2048

using Duthie.Bot;
using Duthie.Bot.Programs;

await ModuleLoader.LoadModules();
await CommandLine.RunAsync(args);
await Daemon.RunAsync(args);