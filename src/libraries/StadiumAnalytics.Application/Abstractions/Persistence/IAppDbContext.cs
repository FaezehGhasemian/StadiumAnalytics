using Microsoft.EntityFrameworkCore;
using StadiumAnalytics.Domain.Entities;

namespace StadiumAnalytics.Application.Abstractions.Persistence
{
    public interface IAppDbContext
    {
        DbSet<SensorEvent> SensorEvents { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
