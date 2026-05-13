using StadiumAnalytics.Domain.Enums;

namespace StadiumAnalytics.Application.Abstractions.Eventing
{
    public sealed record SensorEventMessage(
    string Gate,
    DateTime TimestampUtc,
    int NumberOfPeople,
    MovementType Type);
}
