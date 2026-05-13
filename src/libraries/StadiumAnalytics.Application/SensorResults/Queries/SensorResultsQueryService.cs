using Microsoft.EntityFrameworkCore;
using StadiumAnalytics.Application.Abstractions.Persistence;
using StadiumAnalytics.Application.SensorResults.Dtos;

namespace StadiumAnalytics.Application.SensorResults.Queries;

public sealed class SensorResultsQueryService : ISensorResultsQueryService
{
    private readonly IAppDbContext _db;

    public SensorResultsQueryService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SensorResultDto>> GetGroupedAsync(
        GetSensorResultsQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _db.SensorEvents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Gate))
            q = q.Where(e => e.Gate == query.Gate);

        if (query.Type.HasValue)
            q = q.Where(e => e.Type == query.Type.Value);

        if (query.StartTimeUtc.HasValue)
            q = q.Where(e => e.TimestampUtc >= query.StartTimeUtc.Value);

        if (query.EndTimeUtc.HasValue)
            q = q.Where(e => e.TimestampUtc <= query.EndTimeUtc.Value);

        return await q
            .GroupBy(e => new { e.Gate, e.Type })
            .Select(g => new SensorResultDto(
                g.Key.Gate,
                g.Key.Type,
                g.Sum(x => x.NumberOfPeople)))
            .ToListAsync(cancellationToken);
    }
}