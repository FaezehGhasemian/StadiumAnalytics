using StadiumAnalytics.Domain.Enums;

namespace StadiumAnalytics.Application.SensorResults.Dtos
{
    public sealed record SensorResultDto(
      string Gate,
      MovementType Type,
      int NumberOfPeople);
}
