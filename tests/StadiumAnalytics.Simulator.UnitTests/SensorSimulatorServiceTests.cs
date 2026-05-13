using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StadiumAnalytics.Application.Abstractions.Eventing;
using StadiumAnalytics.Domain.Enums;

namespace StadiumAnalytics.Simulator.UnitTests;

public class SensorSimulatorServiceTests
{
    [Fact]
    public async Task ExecuteAsync_PublishesAtLeastOneSensorEventMessage()
    {
        var published = new List<SensorEventMessage>();
        var bus = new Mock<IEventBus>();
        bus.Setup(b => b.PublishAsync(It.IsAny<SensorEventMessage>(), It.IsAny<CancellationToken>()))
            .Callback<SensorEventMessage, CancellationToken>((m, _) => published.Add(m))
            .Returns(Task.CompletedTask);

        var options = Options.Create(new SimulatorOptions
        {
            Gates = new[] { "Gate A", "Gate B" },
            IntervalMilliseconds = 1,
            MaxPeoplePerEvent = 10
        });

        var service = new SensorSimulatorService(bus.Object, options, NullLogger<SensorSimulatorService>.Instance);
        using var cts = new CancellationTokenSource();

        await service.StartAsync(cts.Token);
        await Task.Delay(150);
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        published.Should().NotBeEmpty();
        published.Should().OnlyContain(m =>
            (m.Gate == "Gate A" || m.Gate == "Gate B") &&
            m.NumberOfPeople >= 0 && m.NumberOfPeople <= 10 &&
            (m.Type == MovementType.Enter || m.Type == MovementType.Leave) &&
            m.TimestampUtc.Kind == DateTimeKind.Utc);
    }

    [Fact]
    public async Task ExecuteAsync_StopsCleanlyWhenCancelled()
    {
        var bus = new Mock<IEventBus>();
        bus.Setup(b => b.PublishAsync(It.IsAny<SensorEventMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var options = Options.Create(new SimulatorOptions { IntervalMilliseconds = 1 });
        var service = new SensorSimulatorService(bus.Object, options, NullLogger<SensorSimulatorService>.Instance);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        cts.Cancel();

        var stop = service.StopAsync(CancellationToken.None);
        var completed = await Task.WhenAny(stop, Task.Delay(TimeSpan.FromSeconds(2)));
        completed.Should().BeSameAs(stop, "the BackgroundService must observe cancellation and stop");
    }

    [Fact]
    public async Task ExecuteAsync_SwallowsTransientPublishErrorsAndContinues()
    {
        var calls = 0;
        var bus = new Mock<IEventBus>();
        bus.Setup(b => b.PublishAsync(It.IsAny<SensorEventMessage>(), It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                if (Interlocked.Increment(ref calls) == 1)
                    throw new InvalidOperationException("transient");
                return Task.CompletedTask;
            });

        var options = Options.Create(new SimulatorOptions { IntervalMilliseconds = 1 });
        var service = new SensorSimulatorService(bus.Object, options, NullLogger<SensorSimulatorService>.Instance);

        using var cts = new CancellationTokenSource();
        await service.StartAsync(cts.Token);
        try { await Task.Delay(200, cts.Token); } catch (OperationCanceledException) { }
        cts.Cancel();
        await service.StopAsync(CancellationToken.None);

        calls.Should().BeGreaterThan(1, "the loop should keep running after a transient publish error");
    }
}
