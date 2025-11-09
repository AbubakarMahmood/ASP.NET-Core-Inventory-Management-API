using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configuration;

/// <summary>
/// Entity configuration for Product
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(p => p.SKU)
            .IsUnique();

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

        builder.Property(p => p.CostingMethod)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(p => p.RowVersion)
            .IsRowVersion();

        // Indexes for common queries
        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.CurrentStock);

        // Relationships
        builder.HasMany(p => p.WorkOrderItems)
            .WithOne(woi => woi.Product)
            .HasForeignKey(woi => woi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(p => p.StockMovements)
            .WithOne(sm => sm.Product)
            .HasForeignKey(sm => sm.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
