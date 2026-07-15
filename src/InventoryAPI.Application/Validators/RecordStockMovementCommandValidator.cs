using FluentValidation;
using InventoryAPI.Application.Commands.StockMovements;
using InventoryAPI.Domain.Enums;

namespace InventoryAPI.Application.Validators;

/// <summary>
/// Validator for RecordStockMovementCommand
/// </summary>
public class RecordStockMovementCommandValidator : AbstractValidator<RecordStockMovementCommand>
{
    public RecordStockMovementCommandValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product id is required");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid movement type");

        // Adjustments carry their own sign; every other movement type must be positive
        RuleFor(x => x.Quantity)
            .NotEqual(0).WithMessage("Quantity cannot be zero");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .When(x => x.Type != StockMovementType.Adjustment)
            .WithMessage("Quantity must be greater than zero for this movement type");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");

        RuleFor(x => x.SourceLocation)
            .MaximumLength(100).WithMessage("Source location cannot exceed 100 characters");

        RuleFor(x => x.DestinationLocation)
            .MaximumLength(100).WithMessage("Destination location cannot exceed 100 characters");

        RuleFor(x => x.DestinationLocation)
            .NotEmpty()
            .When(x => x.Type == StockMovementType.Transfer)
            .WithMessage("Destination location is required for transfers");

        RuleFor(x => x.Reference)
            .MaximumLength(100).WithMessage("Reference cannot exceed 100 characters");
    }
}
