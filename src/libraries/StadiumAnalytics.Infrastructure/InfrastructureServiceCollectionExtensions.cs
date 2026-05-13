using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StadiumAnalytics.Application.Abstractions.Eventing;
using StadiumAnalytics.Application.Abstractions.Persistence;
using StadiumAnalytics.Infrastructure.Eventing;
using StadiumAnalytics.Infrastructure.Persistence;

namespace StadiumAnalytics.Infrastructure;


public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? "Host=localhost;Port=5432;Database=stadium;Username=stadium;Password=stadium";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }

    public static IServiceCollection AddMessaging(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventBus, RabbitMqEventBus>();

        return services;
    }
}
