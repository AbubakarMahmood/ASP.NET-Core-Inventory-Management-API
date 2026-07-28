using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Exceptions;
using MediatR;

namespace InventoryAPI.Application.Commands.Products;

/// <summary>
/// Soft-deletes an unused catalog record. Products that participate in ledger
/// or work-order history are retained so historical evidence remains resolvable.
/// </summary>
public class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product with ID {request.Id} not found");

        var hasLedgerHistory = await _unitOfWork.StockMovements
            .AnyAsync(movement => movement.ProductId == request.Id, cancellationToken);
        var hasWorkOrderHistory = await _unitOfWork.WorkOrderItems
            .AnyAsync(item => item.ProductId == request.Id, cancellationToken);

        if (hasLedgerHistory || hasWorkOrderHistory)
        {
            throw new BusinessRuleViolationException(
                "Products referenced by stock movements or work orders cannot be deleted. Keep the record for historical integrity.");
        }

        _unitOfWork.Products.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}
