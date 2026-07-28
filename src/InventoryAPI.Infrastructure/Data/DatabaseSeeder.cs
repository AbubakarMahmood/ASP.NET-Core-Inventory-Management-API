using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Infrastructure.Data;

/// <summary>
/// Seeds deterministic demonstration data. Seed balances are posted through
/// the same immutable ledger contract used by the application.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordService passwordService)
    {
        if (!await context.Users.AnyAsync())
        {
            var users = new[]
            {
                CreateUser("admin@stockverity.local", "Admin", "User", UserRole.Admin, "Admin123!", passwordService),
                CreateUser("manager@stockverity.local", "Manager", "User", UserRole.Manager, "Manager123!", passwordService),
                CreateUser("operator@stockverity.local", "Operator", "User", UserRole.Operator, "Operator123!", passwordService)
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        var seedActor = await context.Users
            .OrderBy(user => user.Role)
            .FirstAsync(user => user.Role == UserRole.Admin);

        if (!await context.Products.AnyAsync())
        {
            var seeds = new[]
            {
                new ProductSeed("WIDGET-001", "Standard Widget", "A standard widget for general use", "Widgets", 150, 50, 100, "EA", 12.50m, "A-01-01"),
                new ProductSeed("BOLT-M6-50", "M6x50mm Bolt", "M6 bolt, 50mm length, grade 8.8", "Fasteners", 5000, 1000, 5000, "EA", 0.15m, "B-02-05"),
                new ProductSeed("GEAR-42T", "42 Tooth Gear", "Steel gear, 42 teeth, 10mm bore", "Mechanical", 75, 20, 50, "EA", 24.99m, "C-03-12"),
                new ProductSeed("CABLE-ETH-5M", "Ethernet Cable 5m", "Cat6 Ethernet cable, 5 meter length", "Cables", 200, 50, 100, "EA", 8.75m, "D-01-08"),
                new ProductSeed("SEAL-ORNG-100", "O-Ring 100mm", "Nitrile O-ring, 100mm diameter", "Seals", 25, 50, 100, "EA", 3.50m, "E-04-03")
            };

            foreach (var seed in seeds)
            {
                var product = new Product
                {
                    SKU = seed.Sku,
                    Name = seed.Name,
                    Description = seed.Description,
                    Category = seed.Category,
                    ReorderPoint = seed.ReorderPoint,
                    ReorderQuantity = seed.ReorderQuantity,
                    UnitOfMeasure = seed.UnitOfMeasure,
                    UnitCost = seed.UnitCost,
                    Location = seed.Location
                };

                await context.Products.AddAsync(product);
                await context.StockMovements.AddAsync(StockMovement.Post(
                    product,
                    product.Id,
                    StockMovementType.OpeningBalance,
                    seed.OpeningStock,
                    "Demonstration seed opening balance",
                    $"SEED-{seed.Sku}",
                    seedActor.Id));
            }

            await context.SaveChangesAsync();
        }

        if (!await context.WorkOrders.AnyAsync())
        {
            var operatorUser = await context.Users.FirstAsync(user => user.Role == UserRole.Operator);
            var product1 = await context.Products.FirstAsync(product => product.SKU == "WIDGET-001");
            var product2 = await context.Products.FirstAsync(product => product.SKU == "BOLT-M6-50");

            var workOrder = new WorkOrder
            {
                OrderNumber = "WO-DEMO-001",
                Title = "Assembly Line Maintenance",
                Description = "Routine maintenance demonstration work order",
                Priority = WorkOrderPriority.High,
                Status = WorkOrderStatus.Draft,
                DueDate = DateTime.UtcNow.AddDays(7),
                RequestedById = operatorUser.Id,
                Items = new List<WorkOrderItem>
                {
                    new()
                    {
                        ProductId = product1.Id,
                        QuantityRequested = 10
                    },
                    new()
                    {
                        ProductId = product2.Id,
                        QuantityRequested = 50
                    }
                }
            };

            await context.WorkOrders.AddAsync(workOrder);
            await context.SaveChangesAsync();
        }
    }

    private static User CreateUser(
        string email,
        string firstName,
        string lastName,
        UserRole role,
        string password,
        IPasswordService passwordService)
    {
        return new User
        {
            Email = email,
            PasswordHash = passwordService.HashPassword(password),
            FirstName = firstName,
            LastName = lastName,
            Role = role,
            IsActive = true
        };
    }

    private sealed record ProductSeed(
        string Sku,
        string Name,
        string Description,
        string Category,
        int OpeningStock,
        int ReorderPoint,
        int ReorderQuantity,
        string UnitOfMeasure,
        decimal UnitCost,
        string Location);
}
