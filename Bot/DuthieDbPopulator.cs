using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types;
using Microsoft.EntityFrameworkCore;

namespace Duthie.Bot;

internal static class DuthieDbPopulator
{
    public static async Task PopulateAsync(this DuthieDbContext context)
    {
        AppDomain.CurrentDomain.Load("Duthie.Modules");
        await PopulateSitesAsync(context);
        await PopulateLeaguesAsync(context);
    }

    private static async Task PopulateSitesAsync(DuthieDbContext context)
    {
        var a = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.StartsWith("Duthie."));
        var b = a.SelectMany(s => s.GetTypes());
        var c = b.Where(p => typeof(ISiteProvider).IsAssignableFrom(p) && !p.IsAbstract);

        var sites = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => typeof(ISiteProvider).IsAssignableFrom(p) && !p.IsAbstract)
            .SelectMany(p => ((ISiteProvider)Activator.CreateInstance(p)!).Sites);

        foreach (var site in sites)
        {
            if (!await context.Set<Site>().AnyAsync(s => s.Id == site.Id))
                await context.Set<Site>().AddAsync(site);
        }

        await context.SaveChangesAsync();
    }

    private static async Task PopulateLeaguesAsync(DuthieDbContext context)
    {
        var leagues = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(l => l.GetTypes())
            .Where(p => typeof(ILeagueProvider).IsAssignableFrom(p) && !p.IsAbstract)
            .SelectMany(p => ((ILeagueProvider)Activator.CreateInstance(p)!).Leagues);

        foreach (var league in leagues)
        {
            if (!await context.Set<League>().AnyAsync(l => l.Id == league.Id))
                await context.Set<League>().AddAsync(league);
        }

        await context.SaveChangesAsync();
    }
}