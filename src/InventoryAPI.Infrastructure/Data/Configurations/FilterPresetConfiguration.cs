using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for FilterPreset
/// </summary>
public class FilterPresetConfiguration : IEntityTypeConfiguration<FilterPreset>
{
    public void Configure(EntityTypeBuilder<FilterPreset> builder)
    {
        builder.ToTable("FilterPresets");

        builder.HasKey(fp => fp.Id);

        // Configure properties
        builder.Property(fp => fp.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(fp => fp.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(fp => fp.FilterData)
            .IsRequired();

        builder.Property(fp => fp.RowVersion)
            .IsRowVersion();

        // Configure relationships
        builder.HasOne(fp => fp.User)
            .WithMany()
            .HasForeignKey(fp => fp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(fp => fp.UserId);
        builder.HasIndex(fp => fp.EntityType);
        builder.HasIndex(fp => new { fp.UserId, fp.EntityType, fp.Name });
    }
}
