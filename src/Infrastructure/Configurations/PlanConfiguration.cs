using System;
using Domain.Plans;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Infrastructure.Configurations;

public class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToCollection("plans");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Status)
            .HasConversion(
                v => v.Id,
                v => PlanStatus.FromId(v));
    }
}
