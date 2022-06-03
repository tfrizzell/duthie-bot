using Duthie.Data;
using Duthie.Services.Extensions;
using Duthie.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Duthie.Services.Guilds;

public class GuildMessageService
{
    private readonly ILogger<GuildMessageService> _logger;
    private readonly IDbContextFactory<DuthieDbContext> _contextFactory;

    public GuildMessageService(
        ILogger<GuildMessageService> logger,
        IDbContextFactory<DuthieDbContext> contextFactory)
    {
        _logger = logger;
        _contextFactory = contextFactory;
    }

    private IQueryable<GuildMessage> CreateQuery(DuthieDbContext context) =>
        context.Set<GuildMessage>()
            .Include(m => m.Guild)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.GuildId);

    public async Task<int> DeleteAsync(IEnumerable<Guid> ids) =>
        await DeleteAsync(ids.ToArray());

    public async Task<int> DeleteAsync(params Guid[] ids)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var id in ids)
            {
                var message = await context.Set<GuildMessage>().FirstOrDefaultAsync(m => m.Id == id);

                if (message != null)
                    await context.Set<GuildMessage>().RemoveAsync(message);
            }

            return await context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await context.Set<GuildMessage>().AnyAsync(m => m.Id == id);
        }
    }

    public async Task<IEnumerable<GuildMessage>> FindAsync(string text = "", IEnumerable<ulong>? guilds = null)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var query = CreateQuery(context);

            if (!string.IsNullOrWhiteSpace(text))
                query = query.Where(m => m.Id.ToString().ToLower().Equals(text.ToLower()));

            if (guilds?.Count() > 0)
                query = query.Where(m => guilds.Contains(m.GuildId));

            return await query
                .OrderBy(m => m.Id.ToString().ToLower().Equals(text.ToLower()))
                .ThenBy(m => m.CreatedAt)
                .ThenBy(m => m.GuildId)
                .ToListAsync();
        }
    }

    public async Task<GuildMessage?> GetAsync(Guid id)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).FirstOrDefaultAsync(m => m.Id == id);
        }
    }

    public async Task<IEnumerable<GuildMessage>> GetAllAsync()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).ToListAsync();
        }
    }

    public async Task<IEnumerable<GuildMessage>> GetUnsentAsync()
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            return await CreateQuery(context).Where(m => m.SentAt == null).ToListAsync();
        }
    }

    public async Task<int> SaveAsync(IEnumerable<GuildMessage> messages) =>
        await SaveAsync(messages.ToArray());

    public async Task<int> SaveAsync(params GuildMessage[] messages)
    {
        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            foreach (var message in messages)
            {
                if (!await context.Set<GuildMessage>().AnyAsync(m => m.Id == message.Id))
                {
                    message.CreatedAt = DateTimeOffset.UtcNow;
                    await context.Set<GuildMessage>().AddAsync(message);
                }
                else
                    await context.Set<GuildMessage>().UpdateAsync(message);
            }

            return await context.SaveChangesAsync();
        }
    }
}