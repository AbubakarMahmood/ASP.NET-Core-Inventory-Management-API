using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Commands.Products;

/// <summary>
/// Update product command handler
/// </summary>
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products
            .GetByIdAsync(request.Id, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException($"Product with ID {request.Id} not found");
        }

        // Check if SKU already exists for another product
        var existingProduct = await _unitOfWork.Products
            .FirstOrDefaultAsync(p => p.SKU == request.SKU && p.Id != request.Id, cancellationToken);

        if (existingProduct != null)
        {
            throw new ValidationException("SKU", "Product with this SKU already exists");
        }

        // Reject stale edits: the client sends back the Version it read, and a
        // mismatch means someone else changed the product in the meantime.
        if (request.Version.HasValue && request.Version.Value != product.Version)
        {
            throw new ValidationException("Version",
                "The product has been modified by another user. Please refresh and try again.");
        }

        product.SKU = request.SKU;
        product.Name = request.Name;
        product.Description = request.Description;
        product.Category = request.Category;
        product.CurrentStock = request.CurrentStock;
        product.ReorderPoint = request.ReorderPoint;
        product.ReorderQuantity = request.ReorderQuantity;
        product.UnitOfMeasure = request.UnitOfMeasure;
        product.UnitCost = request.UnitCost;
        product.Location = request.Location;
        product.CostingMethod = request.CostingMethod;

        try
        {
            // EF compares against xmin on save, closing the race window between
            // the check above and the write.
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ValidationException("Version",
                "The product has been modified by another user. Please refresh and try again.");
        }

        return product.ToDto();
    }
}
