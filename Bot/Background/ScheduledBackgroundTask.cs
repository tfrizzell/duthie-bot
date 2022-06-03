using System.Text.RegularExpressions;
using Cronos;
using Microsoft.Extensions.Hosting;

namespace Duthie.Bot.Background;

internal class ScheduledBackgroundTask : IHostedService, IDisposable
{
    private CronExpression _cron;
    Func<CancellationToken, Task> _worker;
    private TimeZoneInfo _timeZoneInfo;
    private System.Timers.Timer? _timer;

    public ScheduledBackgroundTask(
        string cronExpression,
        Func<CancellationToken, Task> worker,
        TimeZoneInfo? timeZoneInfo = null)
    {
        _cron = CronExpression.Parse(cronExpression, Regex.Split(cronExpression, @"\s+").Count() > 5 ? CronFormat.IncludeSeconds : CronFormat.Standard);
        _worker = worker;
        _timeZoneInfo = timeZoneInfo ?? TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    }

    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
        await ScheduleAsync(cancellationToken);
    }

    protected virtual async Task ScheduleAsync(CancellationToken cancellationToken)
    {
        var next = _cron.GetNextOccurrence(DateTimeOffset.Now, _timeZoneInfo);

        if (!next.HasValue)
            return;

        var delay = next.Value - DateTimeOffset.Now;

        if (delay.TotalMilliseconds <= 0)
        {
            await ScheduleAsync(cancellationToken);
            return;
        }

        _timer = new System.Timers.Timer(delay.TotalMilliseconds);

        _timer.Elapsed += async (sender, args) =>
        {
            _timer.Dispose();
            _timer = null;

            if (!cancellationToken.IsCancellationRequested)
            {
                await _worker.Invoke(cancellationToken);
            }

            if (!cancellationToken.IsCancellationRequested)
            {
                await ScheduleAsync(cancellationToken);
            }
        };

        _timer.Start();
    }

    public virtual Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Stop();
        return Task.CompletedTask;
    }

    public virtual void Dispose()
    {
        _timer?.Dispose();
    }
}