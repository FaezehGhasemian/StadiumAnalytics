using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StadiumAnalytics.Application.Abstractions.Eventing;
using StadiumAnalytics.Application.Abstractions.Persistence;
using StadiumAnalytics.Infrastructure.Eventing;
using StadiumAnalytics.Infrastructure.Persistence;

namespace StadiumAnalytics.Infrastructure.UnitTests;

public class InfrastructureServiceCollectionExtensionsTests
{
    private static IConfiguration EmptyConfig() =>
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

    [Fact]
    public void AddPersistence_RegistersAppDbContextAndIAppDbContext()
    {
        var services = new ServiceCollection();
        services.AddPersistence(EmptyConfig());
        var sp = services.BuildServiceProvider();

        sp.GetService<AppDbContext>().Should().NotBeNull();
        sp.GetService<IAppDbContext>().Should().NotBeNull();
        sp.GetRequiredService<IAppDbContext>().Should().BeOfType<AppDbContext>();
    }

    [Fact]
    public void AddPersistence_UsesDefaultConnectionStringWhenNotConfigured()
    {
        var services = new ServiceCollection();
        services.AddPersistence(EmptyConfig());
        var sp = services.BuildServiceProvider();

        var act = () => sp.GetRequiredService<AppDbContext>();
        act.Should().NotThrow();
    }

    [Fact]
    public void AddMessaging_RegistersOptionsAndEventBus()
    {
        var dict = new Dictionary<string, string?>
        {
            ["RabbitMq:HostName"] = "test.host",
            ["RabbitMq:Port"]     = "1234"
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(dict).Build();

        var services = new ServiceCollection();
        services.AddMessaging(config);
        var sp = services.BuildServiceProvider();

        var opts = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        opts.HostName.Should().Be("test.host");
        opts.Port.Should().Be(1234);

        sp.GetService<RabbitMqConnection>().Should().NotBeNull();
        sp.GetService<IEventBus>().Should().NotBeNull();
        sp.GetService<IEventBus>().Should().BeOfType<RabbitMqEventBus>();
    }

    [Fact]
    public void AddMessaging_RegistersConnectionAndEventBusAsSingletons()
    {
        var services = new ServiceCollection();
        services.AddMessaging(EmptyConfig());

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(RabbitMqConnection) && d.Lifetime == ServiceLifetime.Singleton);
        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(IEventBus) && d.Lifetime == ServiceLifetime.Singleton);
    }
}
