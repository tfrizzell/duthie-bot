using System.Reflection;

namespace Duthie.Bot;

internal static class ModuleLoader
{
    public static Task LoadModules()
    {
        var modules = new List<string> { };
        var moduleDir = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory) ?? ".", "modules");

        if (Directory.Exists(moduleDir))
            modules.AddRange(Directory.EnumerateFiles(moduleDir, "*.dll"));

        foreach (var module in modules)
            Assembly.LoadFile(module);

        return Task.CompletedTask;
    }
}