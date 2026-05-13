using StadiumAnalytics.Domain.Enums;

namespace StadiumAnalytics.Application.SensorResults.Queries
{
    public sealed record GetSensorResultsQuery(
     string? Gate,
     MovementType? Type,
     DateTime? StartTimeUtc,
     DateTime? EndTimeUtc);
}
