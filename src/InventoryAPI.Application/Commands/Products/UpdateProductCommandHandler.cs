using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Mappings;
using InventoryAPI.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Commands.Products;

public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Product with ID {request.Id} not found");

        var normalizedSku = TextNormalization.Sku(request.SKU);
        var existingProduct = await _unitOfWork.Products.FirstOrDefaultAsync(
            candidate => candidate.SKU == normalizedSku && candidate.Id != request.Id,
            cancellationToken);

        if (existingProduct != null)
        {
            throw new ValidationException("SKU", "Product with this SKU already exists");
        }

        if (!request.Version.HasValue || request.Version.Value != product.Version)
        {
            throw new ConcurrencyConflictException(
                "The product has been modified by another user. Refresh and retry with the latest version.");
        }

        product.SKU = normalizedSku;
        product.Name = TextNormalization.Required(request.Name);
        product.Description = TextNormalization.Optional(request.Description);
        product.Category = TextNormalization.Required(request.Category);
        product.ReorderPoint = request.ReorderPoint;
        product.ReorderQuantity = request.ReorderQuantity;
        product.UnitOfMeasure = TextNormalization.Code(request.UnitOfMeasure);
        product.UnitCost = request.UnitCost;
        product.Location = TextNormalization.Code(request.Location);

        try
        {
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyConflictException(
                "The product has been modified by another user. Refresh and retry with the latest version.");
        }

        return product.ToDto();
    }
}
