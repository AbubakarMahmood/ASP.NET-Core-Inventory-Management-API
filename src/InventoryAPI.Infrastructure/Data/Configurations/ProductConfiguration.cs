using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for Product
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        // Configure properties
        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(p => p.UnitCost)
            .HasPrecision(18, 2);

        builder.Property(p => p.Location)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.CreatedBy)
            .IsRequired();

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        // Configure relationships
        builder.HasMany(p => p.WorkOrderItems)
            .WithOne(woi => woi.Product)
            .HasForeignKey(woi => woi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.StockMovements)
            .WithOne(sm => sm.Product)
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(p => p.SKU)
            .IsUnique();

        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.CurrentStock);
        builder.HasIndex(p => new { p.Category, p.CurrentStock });
    }
}
