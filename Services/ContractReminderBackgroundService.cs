using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeviceMaintenanceSystem.Services;

public class ContractReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ContractReminderBackgroundService> _logger;
    private readonly TimeSpan _runAtTime;
    private readonly int _daysAhead;

    public ContractReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ContractReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _runAtTime = new TimeSpan(8, 0, 0);
        _daysAhead = 30;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Contract Reminder Background Service is starting.");

        if (IsTodayRunPassed())
        {
            _logger.LogInformation("Today's contract reminder time has passed, running immediately after short delay.");
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            await RunReminderAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var delay = GetNextRunDelay();
                _logger.LogInformation("Next contract reminder run scheduled in {Hours} hours.", delay.TotalHours);

                await Task.Delay(delay, stoppingToken);

                await RunReminderAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in contract reminder background service.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }

        _logger.LogInformation("Contract Reminder Background Service is stopping.");
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

    private async Task RunReminderAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting contract reminder job.");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var contractService = scope.ServiceProvider.GetRequiredService<IMaintenanceContractService>();

            var count = await contractService.SendExpiringRemindersAsync(_daysAhead);

            _logger.LogInformation("Contract reminder job completed. Sent {Count} reminders.", count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run contract reminder job.");
            throw;
        }
    }
}
