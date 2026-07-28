using FluentAssertions;
using InventoryAPI.Application.Commands.Products;
using InventoryAPI.Application.Commands.StockMovements;
using InventoryAPI.Application.Commands.Users;
using InventoryAPI.Application.Commands.WorkOrders;
using InventoryAPI.Application.Validators;
using InventoryAPI.Domain.Enums;

namespace InventoryAPI.UnitTests.Validators;

public class RecordStockMovementCommandValidatorTests
{
    private readonly RecordStockMovementCommandValidator _validator = new();

    private static RecordStockMovementCommand ValidCommand() => new()
    {
        OperationId = Guid.NewGuid(),
        ProductId = Guid.NewGuid(),
        Type = StockMovementType.Receipt,
        Quantity = 10,
        Reason = "Restock"
    };

    [Fact]
    public void ValidReceipt_Passes() =>
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void MissingOperationId_Fails()
    {
        var command = ValidCommand();
        command.OperationId = Guid.Empty;

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ZeroQuantity_Fails()
    {
        var command = ValidCommand();
        command.Quantity = 0;

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeQuantity_ForReceipt_Fails()
    {
        var command = ValidCommand();
        command.Quantity = -5;

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeQuantity_ForAdjustment_Passes()
    {
        var command = ValidCommand();
        command.Type = StockMovementType.Adjustment;
        command.Quantity = -5;

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(StockMovementType.Transfer)]
    [InlineData(StockMovementType.OpeningBalance)]
    public void ReservedMovementType_Fails(StockMovementType type)
    {
        var command = ValidCommand();
        command.Type = type;

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void MissingReason_Fails()
    {
        var command = ValidCommand();
        command.Reason = string.Empty;

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}

public class ProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _createValidator = new();
    private readonly UpdateProductCommandValidator _updateValidator = new();

    private static CreateProductCommand ValidCreate() => new()
    {
        SKU = "PART-001",
        Name = "Test part",
        Description = "Test product",
        Category = "Tests",
        OpeningStock = 5,
        ReorderPoint = 1,
        ReorderQuantity = 10,
        UnitOfMeasure = "EA",
        UnitCost = 2.50m,
        Location = "A-01"
    };

    [Fact]
    public void Create_WithOpeningStock_Passes() =>
        _createValidator.Validate(ValidCreate()).IsValid.Should().BeTrue();

    [Fact]
    public void Create_WithNegativeOpeningStock_Fails()
    {
        var command = ValidCreate();
        command.OpeningStock = -1;

        _createValidator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void Update_RequiresConcurrencyVersion()
    {
        var create = ValidCreate();
        var command = new UpdateProductCommand
        {
            Id = Guid.NewGuid(),
            SKU = create.SKU,
            Name = create.Name,
            Description = create.Description,
            Category = create.Category,
            ReorderPoint = create.ReorderPoint,
            ReorderQuantity = create.ReorderQuantity,
            UnitOfMeasure = create.UnitOfMeasure,
            UnitCost = create.UnitCost,
            Location = create.Location,
            Version = null
        };

        _updateValidator.Validate(command).IsValid.Should().BeFalse();

        command.Version = 42;
        _updateValidator.Validate(command).IsValid.Should().BeTrue();
    }
}

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    private static CreateUserCommand ValidCommand() => new()
    {
        Email = "new.user@example.com",
        Password = "Str0ngPassword",
        FirstName = "New",
        LastName = "User",
        Role = UserRole.Operator
    };

    [Fact]
    public void ValidCommand_Passes() =>
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData("short1A")]
    [InlineData("alllowercase1")]
    [InlineData("ALLUPPERCASE1")]
    [InlineData("NoDigitsHere")]
    public void WeakPassword_Fails(string password)
    {
        var command = ValidCommand();
        command.Password = password;

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void InvalidEmail_Fails()
    {
        var command = ValidCommand();
        command.Email = "not-an-email";

        _validator.Validate(command).IsValid.Should().BeFalse();
    }
}

public class WorkOrderCommandValidatorTests
{
    private readonly CreateWorkOrderCommandValidator _createValidator = new();
    private readonly IssueWorkOrderItemsCommandValidator _issueValidator = new();

    [Fact]
    public void CreateWithoutItems_Fails()
    {
        var command = new CreateWorkOrderCommand { Title = "Maintenance" };

        _createValidator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void DuplicateProductLines_Fail()
    {
        var productId = Guid.NewGuid();
        var command = new CreateWorkOrderCommand
        {
            Title = "Maintenance",
            Items =
            {
                new CreateWorkOrderItemRequest { ProductId = productId, QuantityRequested = 1 },
                new CreateWorkOrderItemRequest { ProductId = productId, QuantityRequested = 2 }
            }
        };

        _createValidator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidCreate_Passes()
    {
        var command = new CreateWorkOrderCommand
        {
            Title = "Maintenance",
            Items = { new CreateWorkOrderItemRequest { ProductId = Guid.NewGuid(), QuantityRequested = 3 } }
        };

        _createValidator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void IssueRequiresOperationIdAndUniquePositiveLines()
    {
        var productId = Guid.NewGuid();
        var command = new IssueWorkOrderItemsCommand
        {
            WorkOrderId = Guid.NewGuid(),
            Items =
            {
                new IssueItemRequest { ProductId = productId, Quantity = 1 },
                new IssueItemRequest { ProductId = productId, Quantity = 1 }
            }
        };

        _issueValidator.Validate(command).IsValid.Should().BeFalse();

        command.OperationId = Guid.NewGuid();
        command.Items.RemoveAt(1);
        _issueValidator.Validate(command).IsValid.Should().BeTrue();
    }
}
