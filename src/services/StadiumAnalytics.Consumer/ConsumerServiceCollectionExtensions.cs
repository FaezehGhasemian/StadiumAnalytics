using Microsoft.Extensions.DependencyInjection;

namespace StadiumAnalytics.Consumer;

public static class ConsumerServiceCollectionExtensions
{
    public static IServiceCollection AddSensorEventConsumer(this IServiceCollection services)
    {
        services.AddHostedService<SensorEventConsumer>();
        return services;
    }
}
