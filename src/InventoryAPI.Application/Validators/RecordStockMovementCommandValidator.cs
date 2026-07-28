using FluentValidation;
using InventoryAPI.Application.Commands.StockMovements;
using InventoryAPI.Domain.Enums;

namespace InventoryAPI.Application.Validators;

public class RecordStockMovementCommandValidator : AbstractValidator<RecordStockMovementCommand>
{
    public RecordStockMovementCommandValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty().WithMessage("Operation id is required for retry safety");

        RuleFor(x => x.ProductId).NotEmpty().WithMessage("Product id is required");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Invalid movement type")
            .Must(type => type is not StockMovementType.Transfer and not StockMovementType.OpeningBalance)
            .WithMessage("Manual postings support Receipt, Issue, Adjustment, and Return only");

        RuleFor(x => x.Quantity)
            .NotEqual(0).WithMessage("Quantity cannot be zero");

        RuleFor(x => x.Quantity)
            .GreaterThan(0)
            .When(x => x.Type != StockMovementType.Adjustment)
            .WithMessage("Quantity must be greater than zero for this movement type");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required")
            .MaximumLength(500).WithMessage("Reason cannot exceed 500 characters");

        RuleFor(x => x.Reference)
            .MaximumLength(100).WithMessage("Reference cannot exceed 100 characters");
    }
}
