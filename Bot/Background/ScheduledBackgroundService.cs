using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Duthie.Bot.Background;

public abstract class ScheduledBackgroundService : IHostedService, IDisposable
{
    private readonly ILogger _logger;
    private IEnumerable<ScheduledBackgroundTask> _tasks = new List<ScheduledBackgroundTask>();

    protected ScheduledBackgroundService(ILogger logger)
    {
        _logger = logger;
    }

    protected virtual TimeZoneInfo? TimeZone { get; } = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    protected abstract string[] Schedules { get; }

    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Starting {GetType().Name}");
        await ScheduleAsync(cancellationToken);
    }

    protected virtual async Task ScheduleAsync(CancellationToken cancellationToken)
    {
        _tasks = Schedules.Select(cronExpression => new ScheduledBackgroundTask(cronExpression, ExecuteAsync));
        await Task.WhenAll(_tasks.Select(async t => await t.StartAsync(cancellationToken)));
    }

    public abstract Task ExecuteAsync(CancellationToken? cancellationToken = null);

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug($"Stopping {GetType().Name}");

        foreach (var task in _tasks)
            await task.StopAsync(cancellationToken);
    }

    public void Dispose()
    {
        foreach (var task in _tasks)
            task.Dispose();
    }
}