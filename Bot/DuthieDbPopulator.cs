using Duthie.Data;
using Duthie.Types.Leagues;
using Duthie.Types.Sites;
using Microsoft.EntityFrameworkCore;

namespace Duthie.Bot;

internal static class DuthieDbPopulator
{
    public static async Task PopulateAsync(this DuthieDbContext context)
    {
        await PopulateSitesAsync(context);
        await PopulateLeaguesAsync(context);
    }

    private static async Task PopulateSitesAsync(DuthieDbContext context)
    {
        var sites = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(ISiteProvider).IsAssignableFrom(t))
            .SelectMany(t => ((ISiteProvider)Activator.CreateInstance(t)!).Sites);

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
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsAbstract && typeof(ILeagueProvider).IsAssignableFrom(t))
            .SelectMany(t => ((ILeagueProvider)Activator.CreateInstance(t)!).Leagues);

        foreach (var league in leagues)
        {
            if (!await context.Set<League>().AnyAsync(l => l.Id == league.Id))
                await context.Set<League>().AddAsync(league);
        }

        await context.SaveChangesAsync();
    }
}