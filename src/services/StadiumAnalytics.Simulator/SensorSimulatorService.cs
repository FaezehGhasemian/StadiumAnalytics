using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StadiumAnalytics.Application.Abstractions.Eventing;
using StadiumAnalytics.Domain.Enums;

namespace StadiumAnalytics.Simulator;

public sealed class SensorSimulatorService : BackgroundService
{
    private readonly IEventBus _eventBus;
    private readonly SimulatorOptions _options;
    private readonly ILogger<SensorSimulatorService> _logger;
    private readonly Random _random = new();

    public SensorSimulatorService(
        IEventBus eventBus,
        IOptions<SimulatorOptions> options,
        ILogger<SensorSimulatorService> logger)
    {
        _eventBus = eventBus;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var gate = _options.Gates[_random.Next(_options.Gates.Length)];
                var type = _random.Next(2) == 0 ? MovementType.Enter : MovementType.Leave;
                var numberOfPeople = _random.Next(0, _options.MaxPeoplePerEvent + 1);

                var message = new SensorEventMessage(
                    gate,
                    DateTime.UtcNow,
                    numberOfPeople,
                    type);

                _logger.LogDebug(
                    "Publishing sensor event: Gate={Gate}, Type={Type}, People={People}",
                    gate, type, numberOfPeople);

                await _eventBus.PublishAsync(message, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing sensor event.");
            }

            await Task.Delay(_options.IntervalMilliseconds, stoppingToken).ConfigureAwait(false);
        }
    }
}
