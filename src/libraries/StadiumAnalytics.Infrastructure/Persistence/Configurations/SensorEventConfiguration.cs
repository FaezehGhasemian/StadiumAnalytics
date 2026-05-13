using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StadiumAnalytics.Domain.Entities;

namespace StadiumAnalytics.Infrastructure.Persistence.Configurations;

public class SensorEventConfiguration : IEntityTypeConfiguration<SensorEvent>
{
    public void Configure(EntityTypeBuilder<SensorEvent> builder)
    {
        builder.ToTable("SensorEvents");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Gate)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Type)
            .HasConversion<int>();

        builder.HasIndex(e => new { e.Gate, e.Type, e.TimestampUtc })
            .HasDatabaseName("IX_SensorEvents_Gate_Type_TimestampUtc");
    }
}
