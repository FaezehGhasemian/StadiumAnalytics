using StadiumAnalytics.Application.SensorResults.Queries;
using StadiumAnalytics.Domain.Enums;

namespace StadiumAnalytics.Api.Controllers;

public sealed record GetSensorResultsRequest(
    string? Gate = null,
    MovementType? Type = null,
    DateTimeOffset? StartTimeUtc = null,
    DateTimeOffset? EndTimeUtc = null)
{
    public GetSensorResultsQuery ToQuery() => new(
        Gate,
        Type,
        StartTimeUtc?.UtcDateTime,
        EndTimeUtc?.UtcDateTime);
}
