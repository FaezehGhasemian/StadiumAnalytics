using StadiumAnalytics.Application.SensorResults.Dtos;

namespace StadiumAnalytics.Application.SensorResults.Queries
{
    public interface ISensorResultsQueryService
    {
        Task<IReadOnlyList<SensorResultDto>> GetGroupedAsync(
            GetSensorResultsQuery query,
            CancellationToken cancellationToken = default);
    }
}
