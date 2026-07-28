using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for a single-location catalog item whose CurrentStock
/// value is a cached projection of the append-only stock ledger.
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint("CK_Products_CurrentStock_NonNegative", "\"CurrentStock\" >= 0");
            table.HasCheckConstraint("CK_Products_ReorderPoint_NonNegative", "\"ReorderPoint\" >= 0");
            table.HasCheckConstraint("CK_Products_ReorderQuantity_Positive", "\"ReorderQuantity\" > 0");
            table.HasCheckConstraint("CK_Products_UnitCost_NonNegative", "\"UnitCost\" >= 0");
        });

        builder.HasKey(product => product.Id);

        builder.Property(product => product.SKU)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(product => product.Description)
            .HasMaxLength(1000);

        builder.Property(product => product.Category)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(product => product.UnitOfMeasure)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(product => product.UnitCost)
            .HasPrecision(18, 2);

        builder.Property(product => product.Location)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(product => product.CreatedBy)
            .IsRequired();

        builder.HasIndex(product => product.SKU)
            .IsUnique();

        builder.HasIndex(product => product.Category);
        builder.HasIndex(product => product.CurrentStock);
        builder.HasIndex(product => new { product.Category, product.CurrentStock });
    }
}
