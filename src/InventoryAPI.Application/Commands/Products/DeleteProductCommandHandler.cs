using InventoryAPI.Application.Exceptions;
using InventoryAPI.Application.Interfaces;
using MediatR;

namespace InventoryAPI.Application.Commands.Products;

/// <summary>
/// Delete product command handler
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
        var product = await _unitOfWork.Products
            .GetByIdAsync(request.Id, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException($"Product with ID {request.Id} not found");
        }

        // Soft delete
        _unitOfWork.Products.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
