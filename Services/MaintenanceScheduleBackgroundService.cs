using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeviceMaintenanceSystem.Services;

public class MaintenanceScheduleBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MaintenanceScheduleBackgroundService> _logger;
    private readonly TimeSpan _runAtTime;
    private readonly int _monthsAhead;

    public MaintenanceScheduleBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<MaintenanceScheduleBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _runAtTime = new TimeSpan(2, 0, 0);
        _monthsAhead = 3;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Maintenance Schedule Background Service is starting.");

        if (IsTodayRunPassed())
        {
            _logger.LogInformation("Today's schedule generation time has passed, running immediately after short delay.");
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            await RunGenerationAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetNextRunDelay();
                _logger.LogInformation("Next maintenance schedule generation run scheduled in {Hours} hours.", delay.TotalHours);

                await Task.Delay(delay, stoppingToken);

                await RunGenerationAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in maintenance schedule background service.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Maintenance Schedule Background Service is stopping.");
    }

    private bool IsTodayRunPassed()
    {
        var now = DateTime.Now;
        var todayRun = now.Date + _runAtTime;
        return now >= todayRun;
    }

    private TimeSpan GetNextRunDelay()
    {
        var now = DateTime.Now;
        var todayRun = now.Date + _runAtTime;

        if (now < todayRun)
        {
            return todayRun - now;
        }

        return todayRun.AddDays(1) - now;
    }

    private async Task RunGenerationAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting maintenance schedule generation job.");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scheduleService = scope.ServiceProvider.GetRequiredService<IMaintenanceScheduleService>();

            var count = await scheduleService.GenerateUpcomingPlansAsync(_monthsAhead);

            _logger.LogInformation("Maintenance schedule generation job completed. Generated {Count} plans.", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run maintenance schedule generation job.");
            throw;
        }
    }
}
