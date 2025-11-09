using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configuration;

/// <summary>
/// Entity configuration for User
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        builder.HasIndex(u => u.Email)
            .IsUnique();

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(u => u.RowVersion)
            .IsRowVersion();

        builder.Ignore(u => u.FullName);

        // Relationships
        builder.HasMany(u => u.RequestedWorkOrders)
            .WithOne(wo => wo.RequestedBy)
            .HasForeignKey(wo => wo.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.AssignedWorkOrders)
            .WithOne(wo => wo.AssignedTo!)
            .HasForeignKey(wo => wo.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(u => u.StockMovements)
            .WithOne(sm => sm.PerformedBy)
            .HasForeignKey(sm => sm.PerformedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
