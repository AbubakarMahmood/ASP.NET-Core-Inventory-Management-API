using FluentValidation;
using InventoryAPI.Application.Commands.WorkOrders;

namespace InventoryAPI.Application.Validators;

public class CreateWorkOrderCommandValidator : AbstractValidator<CreateWorkOrderCommand>
{
    public CreateWorkOrderCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters");

        RuleFor(x => x.Priority).IsInEnum().WithMessage("Invalid priority");
        RuleFor(x => x.Items).NotEmpty().WithMessage("A work order requires at least one item");

        RuleFor(x => x.Items)
            .Must(items => items.Select(item => item.ProductId).Distinct().Count() == items.Count)
            .WithMessage("A product may appear only once on a work order");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("Product id is required");
            item.RuleFor(i => i.QuantityRequested)
                .GreaterThan(0).WithMessage("Requested quantity must be greater than zero");
            item.RuleFor(i => i.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
        });
    }
}

public class RejectWorkOrderCommandValidator : AbstractValidator<RejectWorkOrderCommand>
{
    public RejectWorkOrderCommandValidator()
    {
        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("A reason is required to reject a work order")
            .MaximumLength(1000).WithMessage("Reason cannot exceed 1000 characters");
    }
}

public class IssueWorkOrderItemsCommandValidator : AbstractValidator<IssueWorkOrderItemsCommand>
{
    public IssueWorkOrderItemsCommandValidator()
    {
        RuleFor(x => x.OperationId)
            .NotEmpty().WithMessage("Operation id is required for retry safety");

        RuleFor(x => x.WorkOrderId)
            .NotEmpty().WithMessage("Work order id is required");

        RuleFor(x => x.Items).NotEmpty().WithMessage("At least one item must be issued");

        RuleFor(x => x.Items)
            .Must(items => items.Select(item => item.ProductId).Distinct().Count() == items.Count)
            .WithMessage("A product may appear only once in an issue request");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId).NotEmpty().WithMessage("Product id is required");
            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Issue quantity must be greater than zero");
            item.RuleFor(i => i.Notes)
                .MaximumLength(1000).WithMessage("Notes cannot exceed 1000 characters");
        });
    }
}
