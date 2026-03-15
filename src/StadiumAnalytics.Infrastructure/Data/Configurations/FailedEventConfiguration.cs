using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StadiumAnalytics.Infrastructure.Data.Entities;

namespace StadiumAnalytics.Infrastructure.Data.Configurations;

public sealed class FailedEventConfiguration : IEntityTypeConfiguration<FailedEventEntity>
{
    public void Configure(EntityTypeBuilder<FailedEventEntity> builder)
    {
        builder.ToTable("FailedEvents");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.Gate).HasMaxLength(100);
        builder.Property(e => e.Timestamp).HasMaxLength(100);
        builder.Property(e => e.Type).HasMaxLength(50);
        builder.Property(e => e.RawPayload).IsRequired();
        builder.Property(e => e.Reason).HasMaxLength(50).IsRequired();
        builder.Property(e => e.ErrorDetails).HasMaxLength(2000);
        builder.Property(e => e.FailedAtUtc).IsRequired();
    }
}
