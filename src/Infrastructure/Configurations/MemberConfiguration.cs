using System;
using Domain.Members;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MongoDB.EntityFrameworkCore.Extensions;

namespace Infrastructure.Configurations;

/// <summary>
/// Entity Framework Core configuration for the <see cref="Member"/> entity, mapping its properties and relationships to the database schema.
/// </summary>
public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToCollection("members");

        builder.HasKey(m => m.Id);
        builder.HasIndex(m => m.IdentifyName).IsUnique();
        builder.OwnsMany(m => m.ExternalConnections, navigationBuilder =>
        {
            navigationBuilder.WithOwner().HasForeignKey(ec => ec.MemberId);
            navigationBuilder.HasKey(ec => ec.Id);
            navigationBuilder.Property(ec => ec.Id).ValueGeneratedNever();
            navigationBuilder
                .Property(ec => ec.Status)
                .HasConversion(s => s.Id, id => ConnectionStatus.FromId(id));
            navigationBuilder
                .Property(ec => ec.Provider)
                .HasConversion(s => s.Id, id => ExternalProvider.FromId(id));
            navigationBuilder
                .Property(ec => ec.Type)
                .HasConversion(t => t.Id, id => ExternalConnectionType.FromId(id));
        });

        builder.OwnsMany(m => m.PersonalPlans, navigationBuilder =>
        {
            navigationBuilder.WithOwner().HasForeignKey(pp => pp.MemberId);
            navigationBuilder.HasKey(pp => pp.Id);
            navigationBuilder.Property(pp => pp.Id).ValueGeneratedNever();
        });
    }
}
