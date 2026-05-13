using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StadiumAnalytics.Application.SensorResults.Queries;

namespace StadiumAnalytics.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISensorResultsQueryService, SensorResultsQueryService>();
        services.AddValidatorsFromAssemblyContaining<GetSensorResultsQueryValidator>();
        return services;
    }
}
