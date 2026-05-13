using FluentAssertions;
using FluentValidation.TestHelper;
using StadiumAnalytics.Application.SensorResults.Queries;
using StadiumAnalytics.Domain.Enums;

namespace StadiumAnalytics.Application.UnitTests.SensorResults;

public class GetSensorResultsQueryValidatorTests
{
    private readonly GetSensorResultsQueryValidator _validator = new();

    [Fact]
    public void Valid_NoFilters_Passes()
    {
        var result = _validator.TestValidate(new GetSensorResultsQuery(null, null, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Gate_TooLong_Fails()
    {
        var longGate = new string('x', 101);
        var result = _validator.TestValidate(new GetSensorResultsQuery(longGate, null, null, null));
        result.ShouldHaveValidationErrorFor(x => x.Gate);
    }

    [Fact]
    public void Gate_AtBoundary_Passes()
    {
        var gate = new string('x', 100);
        var result = _validator.TestValidate(new GetSensorResultsQuery(gate, null, null, null));
        result.ShouldNotHaveValidationErrorFor(x => x.Gate);
    }

    [Fact]
    public void StartAfterEnd_Fails()
    {
        var start = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc);
        var end   = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = _validator.TestValidate(new GetSensorResultsQuery(null, null, start, end));
        result.ShouldHaveValidationErrorFor(x => x.StartTimeUtc);
        result.ShouldHaveValidationErrorFor(x => x.EndTimeUtc);
    }

    [Fact]
    public void StartEqualsEnd_Passes()
    {
        var t = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = _validator.TestValidate(new GetSensorResultsQuery(null, null, t, t));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OnlyStart_Passes()
    {
        var t = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = _validator.TestValidate(new GetSensorResultsQuery(null, null, t, null));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OnlyEnd_Passes()
    {
        var t = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var result = _validator.TestValidate(new GetSensorResultsQuery(null, null, null, t));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(MovementType.Enter)]
    [InlineData(MovementType.Leave)]
    public void Type_AnyKnownValue_Passes(MovementType t)
    {
        var result = _validator.TestValidate(new GetSensorResultsQuery(null, t, null, null));
        result.ShouldNotHaveAnyValidationErrors();
    }
}
