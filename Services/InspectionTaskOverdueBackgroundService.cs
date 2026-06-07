using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DeviceMaintenanceSystem.Services;

public class InspectionTaskOverdueBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InspectionTaskOverdueBackgroundService> _logger;
    private readonly TimeSpan _checkInterval;

    public InspectionTaskOverdueBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<InspectionTaskOverdueBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _checkInterval = TimeSpan.FromHours(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Inspection Task Overdue Background Service is starting.");

        await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOverdueTasksAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in inspection task overdue background service.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Inspection Task Overdue Background Service is stopping.");
    }

    private async Task CheckOverdueTasksAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting inspection task overdue check.");

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var inspectionTaskService = scope.ServiceProvider.GetRequiredService<IInspectionTaskService>();

            var count = await inspectionTaskService.MarkOverdueTasksAsync();

            if (count > 0)
            {
                _logger.LogInformation("Inspection task overdue check completed. Marked {Count} tasks as overdue.", count);
            }
            else
            {
                _logger.LogInformation("Inspection task overdue check completed. No overdue tasks found.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to run inspection task overdue check.");
            throw;
        }
    }
}
