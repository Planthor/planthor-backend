using System;
using Domain.Members;
using Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Plan"/> entity, detailing its indexes, owned entities, and database mappings.
/// </summary>
public sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToCollection("plans");

        builder.HasKey(p => p.Id);

        // Denormalized: maintained by the domain aggregate on every AddActivityLog / Update call.
        builder.Property(p => p.CurrentValue).IsRequired();

        builder.Property(p => p.Status)
            .HasConversion(
                v => v.Id,
                v => PlanStatus.FromId(v));

        builder.OwnsMany(p => p.ActivityLogs, navigationBuilder =>
        {
            navigationBuilder.WithOwner().HasForeignKey(al => al.PlanId);
            navigationBuilder.HasKey(al => al.Id);
            navigationBuilder.Property(al => al.Id).ValueGeneratedNever();
            
            navigationBuilder.OwnsOne(al => al.ExternalSource, sourceBuilder =>
            {
                // ActivityLog IDs are globally unique. Reuse the ownership FK as the nested
                // value object's key so Mongo EF can track sources for multiple ledger rows.
                sourceBuilder.HasKey("ActivityLogId");
                sourceBuilder.Property(s => s.Provider)
                    .HasConversion(p => p.Id, id => ExternalProvider.FromId(id));
            });
            
            navigationBuilder.Navigation(al => al.ExternalSource).IsRequired(false);
        });
    }
}
