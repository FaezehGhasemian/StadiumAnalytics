using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StadiumAnalytics.Simulator;

namespace StadiumAnalytics.Simulator.UnitTests;

public class SimulatorServiceCollectionExtensionsTests
{
    private static IConfiguration BuildConfig(IDictionary<string, string?>? values = null) =>
        new ConfigurationBuilder().AddInMemoryCollection(values ?? new Dictionary<string, string?>()).Build();

    [Fact]
    public void AddSimulator_RegistersSimulatorOptionsFromConfiguration()
    {
        var config = BuildConfig(new Dictionary<string, string?>
        {
            ["Simulator:IntervalMilliseconds"] = "500",
            ["Simulator:MaxPeoplePerEvent"]    = "42"
        });

        var services = new ServiceCollection();
        services.AddSimulator(config);
        var sp = services.BuildServiceProvider();

        var opts = sp.GetRequiredService<IOptions<SimulatorOptions>>().Value;
        opts.IntervalMilliseconds.Should().Be(500);
        opts.MaxPeoplePerEvent.Should().Be(42);
    }

    [Fact]
    public void AddSimulator_RegistersSensorSimulatorServiceAsHostedService()
    {
        var services = new ServiceCollection();
        services.AddSimulator(BuildConfig());

        services.Should().Contain(d =>
            d.ServiceType == typeof(IHostedService) &&
            d.ImplementationType == typeof(SensorSimulatorService));
    }

    [Fact]
    public void AddSimulator_ReturnsSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddSimulator(BuildConfig());
        result.Should().BeSameAs(services);
    }
}
