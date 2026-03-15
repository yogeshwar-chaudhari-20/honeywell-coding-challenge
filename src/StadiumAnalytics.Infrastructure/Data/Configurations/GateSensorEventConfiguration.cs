using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StadiumAnalytics.Core.Models;
using StadiumAnalytics.Infrastructure.Data.Entities;

namespace StadiumAnalytics.Infrastructure.Data.Configurations;

public sealed class GateSensorEventConfiguration : IEntityTypeConfiguration<GateSensorEventEntity>
{
    public void Configure(EntityTypeBuilder<GateSensorEventEntity> builder)
    {
        builder.ToTable("GateSensorEvents", t =>
        {
            var validGates = string.Join(", ",
                Enum.GetNames<StadiumGate>().Select(g => $"'{g}'"));
            t.HasCheckConstraint("CK_GateSensorEvents_Gate", $"Gate IN ({validGates})");

            var validTypes = string.Join(", ",
                Enum.GetNames<GateEventType>().Select(e => $"'{e}'"));
            t.HasCheckConstraint("CK_GateSensorEvents_Type", $"Type IN ({validTypes})");

            t.HasCheckConstraint("CK_GateSensorEvents_NumberOfPeople", "NumberOfPeople > 0");
        });

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Gate)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Type)
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(e => e.NumberOfPeople)
            .IsRequired();

        builder.Property(e => e.Timestamp)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.CreatedAtUtc)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasIndex(e => new { e.Gate, e.Timestamp, e.Type })
            .IsUnique()
            .HasDatabaseName("IX_GateSensorEvents_Gate_Timestamp_Type");

        builder.HasIndex(e => new { e.Gate, e.Type, e.Timestamp })
            .HasDatabaseName("IX_GateSensorEvents_Gate_Type_Timestamp");
    }
}
