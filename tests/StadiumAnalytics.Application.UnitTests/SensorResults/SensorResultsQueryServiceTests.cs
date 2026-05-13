using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StadiumAnalytics.Application.SensorResults.Queries;
using StadiumAnalytics.Domain.Entities;
using StadiumAnalytics.Domain.Enums;
using StadiumAnalytics.Infrastructure.Persistence;

namespace StadiumAnalytics.Application.UnitTests.SensorResults;

public class SensorResultsQueryServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _db;
    private readonly SensorResultsQueryService _service;
    private readonly DateTime _t0 = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public SensorResultsQueryServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
        _db = new AppDbContext(options);
        _db.Database.EnsureCreated();
        _service = new SensorResultsQueryService(_db);
    }

    private async Task SeedAsync()
    {
        _db.SensorEvents.AddRange(
            new SensorEvent("Gate A", _t0,                  5, MovementType.Enter),
            new SensorEvent("Gate A", _t0.AddMinutes(1),   10, MovementType.Enter),
            new SensorEvent("Gate A", _t0.AddMinutes(2),    3, MovementType.Leave),
            new SensorEvent("Gate B", _t0,                  8, MovementType.Leave),
            new SensorEvent("Gate B", _t0.AddHours(1),      4, MovementType.Enter));
        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task GetGroupedAsync_NoFilter_GroupsByGateAndType()
    {
        await SeedAsync();
        var results = await _service.GetGroupedAsync(new GetSensorResultsQuery(null, null, null, null));

        results.Should().HaveCount(4);
        results.Single(r => r.Gate == "Gate A" && r.Type == MovementType.Enter).NumberOfPeople.Should().Be(15);
        results.Single(r => r.Gate == "Gate A" && r.Type == MovementType.Leave).NumberOfPeople.Should().Be(3);
        results.Single(r => r.Gate == "Gate B" && r.Type == MovementType.Enter).NumberOfPeople.Should().Be(4);
        results.Single(r => r.Gate == "Gate B" && r.Type == MovementType.Leave).NumberOfPeople.Should().Be(8);
    }

    [Fact]
    public async Task GetGroupedAsync_FilterByGate_ReturnsOnlyMatchingGate()
    {
        await SeedAsync();
        var results = await _service.GetGroupedAsync(new GetSensorResultsQuery("Gate A", null, null, null));

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Gate == "Gate A");
    }

    [Fact]
    public async Task GetGroupedAsync_FilterByType_ReturnsOnlyMatchingType()
    {
        await SeedAsync();
        var results = await _service.GetGroupedAsync(new GetSensorResultsQuery(null, MovementType.Leave, null, null));

        results.Should().HaveCount(2);
        results.Should().OnlyContain(r => r.Type == MovementType.Leave);
    }

    [Fact]
    public async Task GetGroupedAsync_FilterByDateRange_BoundsAreInclusive()
    {
        await SeedAsync();
        var results = await _service.GetGroupedAsync(
            new GetSensorResultsQuery(null, null, _t0, _t0.AddMinutes(2)));

        results.Should().Contain(r => r.Gate == "Gate A" && r.Type == MovementType.Enter && r.NumberOfPeople == 15);
        results.Should().Contain(r => r.Gate == "Gate A" && r.Type == MovementType.Leave && r.NumberOfPeople == 3);
        results.Should().Contain(r => r.Gate == "Gate B" && r.Type == MovementType.Leave && r.NumberOfPeople == 8);
        results.Should().NotContain(r => r.Gate == "Gate B" && r.Type == MovementType.Enter);
    }

    [Fact]
    public async Task GetGroupedAsync_NoMatches_ReturnsEmpty()
    {
        await SeedAsync();
        var results = await _service.GetGroupedAsync(new GetSensorResultsQuery("Gate Z", null, null, null));
        results.Should().BeEmpty();
    }

    [Fact]
    public async Task GetGroupedAsync_AllFiltersCombined_AppliesAll()
    {
        await SeedAsync();
        var results = await _service.GetGroupedAsync(
            new GetSensorResultsQuery("Gate A", MovementType.Enter, _t0, _t0.AddMinutes(5)));

        results.Should().ContainSingle();
        results[0].Gate.Should().Be("Gate A");
        results[0].Type.Should().Be(MovementType.Enter);
        results[0].NumberOfPeople.Should().Be(15);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
