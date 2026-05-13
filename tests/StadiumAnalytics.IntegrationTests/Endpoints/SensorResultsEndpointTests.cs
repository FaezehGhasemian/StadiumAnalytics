using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using StadiumAnalytics.Application.SensorResults.Dtos;
using StadiumAnalytics.Domain.Entities;
using StadiumAnalytics.Domain.Enums;
using StadiumAnalytics.Infrastructure.Persistence;
using StadiumAnalytics.IntegrationTests.Fixtures;

namespace StadiumAnalytics.IntegrationTests.Endpoints;

public class SensorResultsEndpointTests : IClassFixture<ApiTestFactory>
{
    private const string Route = "/api/sensor-results/query";

    private readonly ApiTestFactory _factory;
    private readonly DateTime _t0 = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public SensorResultsEndpointTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    private async Task SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.SensorEvents.RemoveRange(db.SensorEvents);
        await db.SaveChangesAsync();

        db.SensorEvents.AddRange(
            new SensorEvent("Gate A", _t0,                  5, MovementType.Enter),
            new SensorEvent("Gate A", _t0.AddMinutes(1),   10, MovementType.Enter),
            new SensorEvent("Gate A", _t0.AddMinutes(2),    3, MovementType.Leave),
            new SensorEvent("Gate B", _t0,                  8, MovementType.Leave),
            new SensorEvent("Gate B", _t0.AddHours(1),      4, MovementType.Enter));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Post_EmptyBody_ReturnsAllGroupedResults()
    {
        await SeedAsync();
        var client = _factory.CreateClient();

        var response = await client.PostJsonAsync(Route, new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadJsonAsync<List<SensorResultDto>>();
        results!.Should().HaveCount(4);
    }

    [Fact]
    public async Task Post_NullBody_ReturnsAllGroupedResults()
    {
        await SeedAsync();
        var client = _factory.CreateClient();

        var response = await client.PostRawAsync(Route, "null");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadJsonAsync<List<SensorResultDto>>();
        results!.Should().HaveCount(4);
    }

    [Fact]
    public async Task Post_FilterByGate_ReturnsOnlyMatchingGate()
    {
        await SeedAsync();
        var client = _factory.CreateClient();

        var response = await client.PostJsonAsync(Route, new { gate = "Gate A" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadJsonAsync<List<SensorResultDto>>();
        results!.Should().HaveCount(2).And.OnlyContain(r => r.Gate == "Gate A");
    }

    [Fact]
    public async Task Post_FilterByType_ReturnsOnlyMatchingType()
    {
        await SeedAsync();
        var client = _factory.CreateClient();

        var response = await client.PostJsonAsync(Route, new { type = "Leave" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadJsonAsync<List<SensorResultDto>>();
        results!.Should().HaveCount(2).And.OnlyContain(r => r.Type == MovementType.Leave);
    }

    [Fact]
    public async Task Post_FilterByDateRange_AppliesInclusiveBounds()
    {
        await SeedAsync();
        var client = _factory.CreateClient();

        var body = new
        {
            startTimeUtc = _t0.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            endTimeUtc   = _t0.AddMinutes(2).ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var response = await client.PostJsonAsync(Route, body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadJsonAsync<List<SensorResultDto>>();
        results!.Should().NotContain(r => r.Gate == "Gate B" && r.Type == MovementType.Enter);
        results.Should().Contain(r => r.Gate == "Gate A" && r.Type == MovementType.Enter && r.NumberOfPeople == 15);
    }

    [Fact]
    public async Task Post_AllFiltersCombined_ReturnsSingleAggregate()
    {
        await SeedAsync();
        var client = _factory.CreateClient();

        var body = new
        {
            gate = "Gate A",
            type = "Enter",
            startTimeUtc = _t0.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            endTimeUtc   = _t0.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
        var response = await client.PostJsonAsync(Route, body);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadJsonAsync<List<SensorResultDto>>();
        results!.Should().ContainSingle();
        results![0].Gate.Should().Be("Gate A");
        results![0].Type.Should().Be(MovementType.Enter);
        results![0].NumberOfPeople.Should().Be(15);
    }

    [Fact]
    public async Task Post_NoMatches_ReturnsEmpty()
    {
        await SeedAsync();
        var client = _factory.CreateClient();

        var response = await client.PostJsonAsync(Route, new { gate = "Gate Z" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await response.Content.ReadJsonAsync<List<SensorResultDto>>();
        results!.Should().BeEmpty();
    }

    [Fact]
    public async Task Post_StartAfterEnd_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var body = new
        {
            startTimeUtc = "2030-01-02T00:00:00Z",
            endTimeUtc   = "2030-01-01T00:00:00Z"
        };
        var response = await client.PostJsonAsync(Route, body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_InvalidMovementType_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostRawAsync(Route, "{\"type\":\"fly\"}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_GateTooLong_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();
        var longGate = new string('x', 101);

        var response = await client.PostJsonAsync(Route, new { gate = longGate });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_MalformedJsonBody_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostRawAsync(Route, "{ not json");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_ResponseIsJson()
    {
        await SeedAsync();
        var client = _factory.CreateClient();

        var response = await client.PostJsonAsync(Route, new { });

        response.IsSuccessStatusCode.Should().BeTrue();
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
    }
}
