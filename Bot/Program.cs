using Duthie.Bot;
using Duthie.Bot.Programs;

await ModuleLoader.LoadModules();
await CommandLine.RunAsync(args);
await Daemon.RunAsync(args);