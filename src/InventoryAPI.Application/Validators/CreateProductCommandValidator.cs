using FluentValidation;
using InventoryAPI.Application.Commands.Products;

namespace InventoryAPI.Application.Validators;

/// <summary>
/// Validator for CreateProductCommand
/// </summary>
public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(x => x.SKU)
            .NotEmpty().WithMessage("SKU is required")
            .MaximumLength(50).WithMessage("SKU cannot exceed 50 characters");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(200).WithMessage("Name cannot exceed 200 characters");

        RuleFor(x => x.Category)
            .NotEmpty().WithMessage("Category is required")
            .MaximumLength(100).WithMessage("Category cannot exceed 100 characters");

        RuleFor(x => x.CurrentStock)
            .GreaterThanOrEqualTo(0).WithMessage("Current stock cannot be negative");

        RuleFor(x => x.ReorderPoint)
            .GreaterThanOrEqualTo(0).WithMessage("Reorder point cannot be negative");

        RuleFor(x => x.ReorderQuantity)
            .GreaterThan(0).WithMessage("Reorder quantity must be greater than zero");

        RuleFor(x => x.UnitOfMeasure)
            .NotEmpty().WithMessage("Unit of measure is required")
            .MaximumLength(20).WithMessage("Unit of measure cannot exceed 20 characters");

        RuleFor(x => x.UnitCost)
            .GreaterThan(0).WithMessage("Unit cost must be greater than zero");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required")
            .MaximumLength(100).WithMessage("Location cannot exceed 100 characters");
    }
}
