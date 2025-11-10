using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configuration;

/// <summary>
/// Entity configuration for FilterPreset
/// </summary>
public class FilterPresetConfiguration : IEntityTypeConfiguration<FilterPreset>
{
    public void Configure(EntityTypeBuilder<FilterPreset> builder)
    {
        builder.ToTable("FilterPresets");

        builder.HasKey(fp => fp.Id);

        builder.Property(fp => fp.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(fp => fp.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(fp => fp.FilterData)
            .IsRequired();

        // Configure optimistic concurrency with RowVersion
        builder.Property(fp => fp.RowVersion)
            .IsRowVersion();

        // Indexes for performance
        builder.HasIndex(fp => new { fp.UserId, fp.EntityType });
        builder.HasIndex(fp => new { fp.UserId, fp.EntityType, fp.IsDefault });

        // Relationships
        builder.HasOne(fp => fp.User)
            .WithMany()
            .HasForeignKey(fp => fp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
