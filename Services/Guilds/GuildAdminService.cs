using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Guilds;

public class GuildAdminService
{
    private readonly ILogger<GuildAdminService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;

    public GuildAdminService(
        ILogger<GuildAdminService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;
    }

    private IQueryable<GuildAdmin> CreateQuery(DuthieDbContext context) =>
        context.Set<GuildAdmin>();

    public async Task<int> DeleteAsync(ulong guildId, params ulong[] memberIds)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var id in memberIds)
            {
                var admin = await context.Set<GuildAdmin>().FirstOrDefaultAsync(a => a.GuildId == guildId && a.MemberId == guildId);

                if (admin != null)
                    await context.Set<GuildAdmin>().RemoveAsync(admin);
            }

            return await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(ulong guildId, ulong memberId)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await context.Set<GuildAdmin>()
                .Include(a => a.Guild)
                .AnyAsync(a => a.GuildId == guildId && a.MemberId == memberId && a.Guild.LeftAt == null);
        }
    }

    public async Task<IEnumerable<ulong>> GetAllAsync(ulong guildId)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context)
                .Where(a => a.GuildId == guildId && a.Guild.LeftAt == null)
                .Select(a => a.MemberId)
                .ToListAsync();
        }
    }

    public async Task<int> SaveAsync(ulong guildId, params ulong[] memberIds)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var memberId in memberIds)
            {
                if (!await context.Set<GuildAdmin>().AnyAsync(a => a.GuildId == guildId && a.MemberId == memberId))
                {
                    await context.Set<GuildAdmin>().AddAsync(new GuildAdmin
                    {
                        GuildId = guildId,
                        MemberId = memberId
                    });
                }
            }

            return await context.SaveChangesAsync();
        }
    }
}