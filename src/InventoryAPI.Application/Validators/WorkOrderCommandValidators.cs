using FluentValidation;
using InventoryAPI.Application.Commands.WorkOrders;

namespace InventoryAPI.Application.Validators;

/// <summary>
/// Validator for CreateWorkOrderCommand
/// </summary>
public class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.Priority)
            .IsInEnum().WithMessage("Invalid priority");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("A work order requires at least one item");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product id is required");

            item.RuleFor(i => i.QuantityRequested)
                .GreaterThan(0).WithMessage("Requested quantity must be greater than zero");

            item.RuleFor(i => i.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
        });
    }
}

/// <summary>
/// Validator for RejectWorkOrderCommand
/// </summary>
public class RejectWorkOrderCommandValidator : AbstractValidator<RejectWorkOrderCommand>
{
    public RejectWorkOrderCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required to reject a work order")
            .MaximumLength(1000).WithMessage("Reason cannot exceed 1000 characters");
    }
}

/// <summary>
/// Validator for IssueWorkOrderItemsCommand
/// </summary>
public class IssueWorkOrderItemsCommandValidator : AbstractValidator<IssueWorkOrderItemsCommand>
{
    public IssueWorkOrderItemsCommandValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item must be issued");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Product id is required");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Issue quantity must be greater than zero");
        });
    }
}
