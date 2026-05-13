using FluentAssertions;
using StadiumAnalytics.Domain.Entities;
using StadiumAnalytics.Domain.Enums;

namespace StadiumAnalytics.Domain.UnitTests;

public class SensorEventTests
{
    private static readonly DateTime ValidUtcTime = DateTime.UtcNow;

    [Fact]
    public void Constructor_ZeroPeople_IsAllowed()
    {
        var act = () => new SensorEvent("Gate A", ValidUtcTime, 0, MovementType.Enter);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_NegativePeople_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SensorEvent("Gate A", ValidUtcTime, -1, MovementType.Enter);
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("numberOfPeople");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_EmptyOrWhitespaceGate_ThrowsArgumentException(string gate)
    {
        var act = () => new SensorEvent(gate, ValidUtcTime, 5, MovementType.Enter);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("gate");
    }

    [Fact]
    public void Constructor_NullGate_ThrowsArgumentException()
    {
        var act = () => new SensorEvent(null!, ValidUtcTime, 5, MovementType.Enter);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("gate");
    }

    [Fact]
    public void Constructor_NonUtcTimestamp_ThrowsArgumentException()
    {
        var localTime = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local);
        var act = () => new SensorEvent("Gate A", localTime, 5, MovementType.Enter);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("timestampUtc");
    }

    [Fact]
    public void Constructor_UnspecifiedKindTimestamp_ThrowsArgumentException()
    {
        var unspec = DateTime.SpecifyKind(ValidUtcTime, DateTimeKind.Unspecified);
        var act = () => new SensorEvent("Gate A", unspec, 5, MovementType.Enter);
        act.Should().Throw<ArgumentException>()
            .WithParameterName("timestampUtc");
    }

    [Fact]
    public void Constructor_GateIsTrimmed()
    {
        var sensorEvent = new SensorEvent("  Gate A  ", ValidUtcTime, 5, MovementType.Enter);
        sensorEvent.Gate.Should().Be("Gate A");
    }

    [Fact]
    public void Constructor_PropertiesAreSet()
    {
        var sensorEvent = new SensorEvent("Gate A", ValidUtcTime, 7, MovementType.Leave);
        sensorEvent.Gate.Should().Be("Gate A");
        sensorEvent.TimestampUtc.Should().Be(ValidUtcTime);
        sensorEvent.NumberOfPeople.Should().Be(7);
        sensorEvent.Type.Should().Be(MovementType.Leave);
    }
}
