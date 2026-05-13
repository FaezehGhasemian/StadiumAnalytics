using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace StadiumAnalytics.Simulator;

public static class SimulatorServiceCollectionExtensions
{
    public static IServiceCollection AddSimulator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<SimulatorOptions>(
            configuration.GetSection(SimulatorOptions.SectionName));

        services.AddHostedService<SensorSimulatorService>();

        return services;
    }
}
