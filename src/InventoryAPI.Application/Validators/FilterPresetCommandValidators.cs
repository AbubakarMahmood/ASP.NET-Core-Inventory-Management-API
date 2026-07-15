using FluentValidation;
using InventoryAPI.Application.Commands.FilterPresets;

namespace InventoryAPI.Application.Validators;

/// <summary>
/// Validator for CreateFilterPresetCommand
/// </summary>
public class CreateFilterPresetCommandValidator : AbstractValidator<CreateFilterPresetCommand>
{
    private static readonly string[] AllowedEntityTypes = { "Product", "WorkOrder", "AuditLog", "StockMovement" };

    public CreateFilterPresetCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.EntityType)
            .NotEmpty().WithMessage("Entity type is required")
            .Must(t => AllowedEntityTypes.Contains(t))
            .WithMessage($"Entity type must be one of: {string.Join(", ", AllowedEntityTypes)}");

        RuleFor(x => x.FilterData)
            .NotEmpty().WithMessage("Filter data is required");
    }
}

/// <summary>
/// Validator for UpdateFilterPresetCommand
/// </summary>
public class UpdateFilterPresetCommandValidator : AbstractValidator<UpdateFilterPresetCommand>
{
    public UpdateFilterPresetCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Filter preset id is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(100).WithMessage("Name cannot exceed 100 characters");

        RuleFor(x => x.FilterData)
            .NotEmpty().WithMessage("Filter data is required");
    }
}
