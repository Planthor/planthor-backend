using System;
using Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Infrastructure.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToCollection("members");

        builder.HasKey(m => m.Id);
        builder.OwnsMany(m => m.ExternalConnections, navigationBuilder =>
        {
            navigationBuilder.Property(ec => ec.Id).ValueGeneratedNever();
            navigationBuilder
                .Property(ec => ec.Status)
                .HasConversion(s => s.Id, id => ConnectionStatus.FromId(id));
            navigationBuilder
                .Property(ec => ec.Provider)
                .HasConversion(s => s.Id, id => ExternalProvider.FromId(id));
        });
    }
}
