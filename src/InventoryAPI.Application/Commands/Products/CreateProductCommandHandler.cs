using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Exceptions;
using InventoryAPI.Application.Interfaces;
using MediatR;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Commands.Products;

/// <summary>
/// Create product command handler
/// </summary>
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // Check if SKU already exists
        var existingProduct = await _unitOfWork.Products
            .FirstOrDefaultAsync(p => p.SKU == request.SKU, cancellationToken);

        if (existingProduct != null)
        {
            throw new ValidationException("SKU", "Product with this SKU already exists");
        }

        var product = request.ToEntity();
        await _unitOfWork.Products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }
}
