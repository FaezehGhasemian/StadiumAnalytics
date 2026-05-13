using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using StadiumAnalytics.Application.SensorResults.Queries;

namespace StadiumAnalytics.Application.UnitTests;

public class ApplicationServiceCollectionExtensionsTests
{
    [Fact]
    public void AddApplication_RegistersSensorResultsQueryService_AsScoped()
    {
        var services = new ServiceCollection();
        services.AddApplication();

        services.Should().ContainSingle(d =>
            d.ServiceType == typeof(ISensorResultsQueryService) &&
            d.ImplementationType == typeof(SensorResultsQueryService) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddApplication_RegistersGetSensorResultsQueryValidator()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        var sp = services.BuildServiceProvider();

        var validator = sp.GetService<IValidator<GetSensorResultsQuery>>();
        validator.Should().NotBeNull();
        validator.Should().BeOfType<GetSensorResultsQueryValidator>();
    }

    [Fact]
    public void AddApplication_ReturnsSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();
        var result = services.AddApplication();

        result.Should().BeSameAs(services);
    }
}
