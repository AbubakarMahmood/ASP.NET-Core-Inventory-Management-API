using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;

namespace InventoryAPI.Infrastructure.Data;

/// <summary>
/// Seeds initial data for development and testing
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context, IPasswordService passwordService)
    {
        // Ensure database and tables exist
        try
        {
            await context.Database.EnsureCreatedAsync();
        }
        catch
        {
            // Tables may already exist, continue
        }

        // Seed Users
        if (!context.Users.Any())
        {
            var users = new[]
            {
                new User
                {
                    Email = "admin@inventory.com",
                    PasswordHash = passwordService.HashPassword("Admin123!"),
                    FirstName = "Admin",
                    LastName = "User",
                    Role = UserRole.Admin,
                    IsActive = true
                },
                new User
                {
                    Email = "manager@inventory.com",
                    PasswordHash = passwordService.HashPassword("Manager123!"),
                    FirstName = "Manager",
                    LastName = "User",
                    Role = UserRole.Manager,
                    IsActive = true
                },
                new User
                {
                    Email = "operator@inventory.com",
                    PasswordHash = passwordService.HashPassword("Operator123!"),
                    FirstName = "Operator",
                    LastName = "User",
                    Role = UserRole.Operator,
                    IsActive = true
                }
            };

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
        }

        // Seed Products
        if (!context.Products.Any())
        {
            var products = new[]
            {
                new Product
                {
                    SKU = "WIDGET-001",
                    Name = "Standard Widget",
                    Description = "A standard widget for general use",
                    Category = "Widgets",
                    CurrentStock = 150,
                    ReorderPoint = 50,
                    ReorderQuantity = 100,
                    UnitOfMeasure = "EA",
                    UnitCost = 12.50m,
                    Location = "A-01-01",
                    CostingMethod = CostingMethod.FIFO
                },
                new Product
                {
                    SKU = "BOLT-M6-50",
                    Name = "M6x50mm Bolt",
                    Description = "M6 bolt, 50mm length, grade 8.8",
                    Category = "Fasteners",
                    CurrentStock = 5000,
                    ReorderPoint = 1000,
                    ReorderQuantity = 5000,
                    UnitOfMeasure = "EA",
                    UnitCost = 0.15m,
                    Location = "B-02-05",
                    CostingMethod = CostingMethod.Average
                },
                new Product
                {
                    SKU = "GEAR-42T",
                    Name = "42 Tooth Gear",
                    Description = "Steel gear, 42 teeth, 10mm bore",
                    Category = "Mechanical",
                    CurrentStock = 75,
                    ReorderPoint = 20,
                    ReorderQuantity = 50,
                    UnitOfMeasure = "EA",
                    UnitCost = 24.99m,
                    Location = "C-03-12",
                    CostingMethod = CostingMethod.FIFO
                },
                new Product
                {
                    SKU = "CABLE-ETH-5M",
                    Name = "Ethernet Cable 5m",
                    Description = "Cat6 Ethernet cable, 5 meter length",
                    Category = "Cables",
                    CurrentStock = 200,
                    ReorderPoint = 50,
                    ReorderQuantity = 100,
                    UnitOfMeasure = "EA",
                    UnitCost = 8.75m,
                    Location = "D-01-08",
                    CostingMethod = CostingMethod.Average
                },
                new Product
                {
                    SKU = "SEAL-ORNG-100",
                    Name = "O-Ring 100mm",
                    Description = "Nitrile O-ring, 100mm diameter",
                    Category = "Seals",
                    CurrentStock = 25,
                    ReorderPoint = 50,
                    ReorderQuantity = 100,
                    UnitOfMeasure = "EA",
                    UnitCost = 3.50m,
                    Location = "E-04-03",
                    CostingMethod = CostingMethod.FIFO
                }
            };

            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();
        }

        // Seed Work Orders
        if (!context.WorkOrders.Any())
        {
            var operator1 = context.Users.First(u => u.Role == UserRole.Operator);
            var manager = context.Users.First(u => u.Role == UserRole.Manager);
            var product1 = context.Products.First(p => p.SKU == "WIDGET-001");
            var product2 = context.Products.First(p => p.SKU == "BOLT-M6-50");

            var workOrder = new WorkOrder
            {
                OrderNumber = "WO-2024-001",
                Title = "Assembly Line Maintenance",
                Description = "Routine maintenance for assembly line A",
                Priority = WorkOrderPriority.High,
                Status = WorkOrderStatus.Draft,
                DueDate = DateTime.UtcNow.AddDays(7),
                RequestedById = operator1.Id,
                Items = new List<WorkOrderItem>
                {
                    new WorkOrderItem
                    {
                        ProductId = product1.Id,
                        QuantityRequested = 10,
                        QuantityIssued = 0
                    },
                    new WorkOrderItem
                    {
                        ProductId = product2.Id,
                        QuantityRequested = 50,
                        QuantityIssued = 0
                    }
                }
            };

            await context.WorkOrders.AddAsync(workOrder);
            await context.SaveChangesAsync();
        }
    }
}
