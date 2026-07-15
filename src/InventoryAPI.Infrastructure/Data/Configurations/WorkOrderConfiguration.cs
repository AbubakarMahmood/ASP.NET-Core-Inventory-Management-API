using InventoryAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace InventoryAPI.Infrastructure.Data.Configurations;

/// <summary>
/// Entity configuration for WorkOrder
/// </summary>
public class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders");

        builder.HasKey(wo => wo.Id);

        builder.Property(wo => wo.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(wo => wo.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(wo => wo.Description)
            .HasMaxLength(2000);

        builder.Property(wo => wo.RejectionReason)
            .HasMaxLength(1000);

        builder.Property(wo => wo.CreatedBy)
            .IsRequired();

        // Relationships
        builder.HasOne(wo => wo.RequestedBy)
            .WithMany(u => u.RequestedWorkOrders)
            .HasForeignKey(wo => wo.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(wo => wo.AssignedTo)
            .WithMany(u => u.AssignedWorkOrders)
            .HasForeignKey(wo => wo.AssignedToId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // Indexes
        builder.HasIndex(wo => wo.OrderNumber)
            .IsUnique();

        builder.HasIndex(wo => wo.Status);
        builder.HasIndex(wo => wo.Priority);
        builder.HasIndex(wo => wo.RequestedById);
        builder.HasIndex(wo => wo.AssignedToId);
        builder.HasIndex(wo => wo.DueDate);
    }
}
