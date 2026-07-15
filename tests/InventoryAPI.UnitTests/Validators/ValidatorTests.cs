using FluentAssertions;
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
        ProductId = Guid.NewGuid(),
        Type = StockMovementType.Receipt,
        Quantity = 10,
        Reason = "Restock"
    };

    [Fact]
    public void ValidCommand_Passes()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
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

    [Fact]
    public void Transfer_WithoutDestination_Fails()
    {
        var command = ValidCommand();
        command.Type = StockMovementType.Transfer;
        command.DestinationLocation = string.Empty;

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
    public void ValidCommand_Passes()
    {
        _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("short1A")]        // too short
    [InlineData("alllowercase1")]  // no uppercase
    [InlineData("ALLUPPERCASE1")]  // no lowercase
    [InlineData("NoDigitsHere")]   // no digit
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

public class CreateWorkOrderCommandValidatorTests
{
    private readonly CreateWorkOrderCommandValidator _validator = new();

    [Fact]
    public void WithoutItems_Fails()
    {
        var command = new CreateWorkOrderCommand { Title = "Maintenance" };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ZeroQuantityItem_Fails()
    {
        var command = new CreateWorkOrderCommand
        {
            Title = "Maintenance",
            Items = { new CreateWorkOrderItemRequest { ProductId = Guid.NewGuid(), QuantityRequested = 0 } }
        };

        _validator.Validate(command).IsValid.Should().BeFalse();
    }

    [Fact]
    public void ValidCommand_Passes()
    {
        var command = new CreateWorkOrderCommand
        {
            Title = "Maintenance",
            Items = { new CreateWorkOrderItemRequest { ProductId = Guid.NewGuid(), QuantityRequested = 3 } }
        };

        _validator.Validate(command).IsValid.Should().BeTrue();
    }
}
