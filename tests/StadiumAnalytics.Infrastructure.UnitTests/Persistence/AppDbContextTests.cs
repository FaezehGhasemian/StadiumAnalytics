using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using StadiumAnalytics.Domain.Entities;
using StadiumAnalytics.Domain.Enums;
using StadiumAnalytics.Infrastructure.Persistence;

namespace StadiumAnalytics.Infrastructure.UnitTests.Persistence;

public class AppDbContextTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public AppDbContextTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options;
    }

    private AppDbContext CreateContext()
    {
        var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
        return ctx;
    }

    [Fact]
    public void Model_HasSensorEventsTable()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(SensorEvent));
        entity.Should().NotBeNull();
        entity!.GetTableName().Should().Be("SensorEvents");
    }

    [Fact]
    public void Model_GateIsRequired_WithMaxLength100()
    {
        using var ctx = CreateContext();
        var prop = ctx.Model.FindEntityType(typeof(SensorEvent))!
            .FindProperty(nameof(SensorEvent.Gate))!;

        prop.IsNullable.Should().BeFalse();
        prop.GetMaxLength().Should().Be(100);
    }

    [Fact]
    public void Model_HasCompositeIndex_OnGateTypeTimestamp()
    {
        using var ctx = CreateContext();
        var entity = ctx.Model.FindEntityType(typeof(SensorEvent))!;
        var index = entity.GetIndexes().SingleOrDefault(i =>
            i.GetDatabaseName() == "IX_SensorEvents_Gate_Type_TimestampUtc");

        index.Should().NotBeNull();
        index!.Properties.Select(p => p.Name)
            .Should().ContainInOrder(nameof(SensorEvent.Gate), nameof(SensorEvent.Type), nameof(SensorEvent.TimestampUtc));
    }

    [Fact]
    public void Model_TypeIsConvertedToInt()
    {
        using var ctx = CreateContext();
        var prop = ctx.Model.FindEntityType(typeof(SensorEvent))!
            .FindProperty(nameof(SensorEvent.Type))!;

        prop.ClrType.Should().Be<MovementType>();
        var converter = prop.GetValueConverter();
        if (converter is not null)
        {
            converter.ProviderClrType.Should().Be<int>();
        }
    }

    [Fact]
    public async Task SaveAndRead_SensorEvent_RoundTrips()
    {
        var ts = DateTime.SpecifyKind(new DateTime(2025, 6, 1, 12, 0, 0), DateTimeKind.Utc);

        await using (var ctx = CreateContext())
        {
            ctx.SensorEvents.Add(new SensorEvent("Gate Q", ts, 42, MovementType.Enter));
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = new AppDbContext(_options))
        {
            var loaded = await ctx.SensorEvents.SingleAsync();
            loaded.Gate.Should().Be("Gate Q");
            loaded.NumberOfPeople.Should().Be(42);
            loaded.Type.Should().Be(MovementType.Enter);
            loaded.Id.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void IAppDbContext_IsImplementedByAppDbContext()
    {
        using var ctx = CreateContext();
        ctx.Should().BeAssignableTo<Application.Abstractions.Persistence.IAppDbContext>();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }
}
