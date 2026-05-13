using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StadiumAnalytics.Consumer;

namespace StadiumAnalytics.Consumer.UnitTests;

public class ConsumerServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSensorEventConsumer_RegistersSensorEventConsumerAsHostedService()
    {
        var services = new ServiceCollection();
        services.AddSensorEventConsumer();

        services.Should().Contain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(SensorEventConsumer));
    }

    [Fact]
    public void AddSensorEventConsumer_ReturnsSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddSensorEventConsumer();
        result.Should().BeSameAs(services);
    }
}
