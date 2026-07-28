using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Mappings;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using MediatR;

namespace InventoryAPI.Application.Commands.Products;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateProductCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ProductDto> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var normalizedSku = TextNormalization.Sku(request.SKU);
        var existingProduct = await _unitOfWork.Products
            .FirstOrDefaultAsync(product => product.SKU == normalizedSku, cancellationToken);

        if (existingProduct != null)
        {
            throw new ValidationException("SKU", "Product with this SKU already exists");
        }

        var product = new Product
        {
            SKU = normalizedSku,
            Name = TextNormalization.Required(request.Name),
            Description = TextNormalization.Optional(request.Description),
            Category = TextNormalization.Required(request.Category),
            ReorderPoint = request.ReorderPoint,
            ReorderQuantity = request.ReorderQuantity,
            UnitOfMeasure = TextNormalization.Code(request.UnitOfMeasure),
            UnitCost = request.UnitCost,
            Location = TextNormalization.Code(request.Location)
        };

        await _unitOfWork.Products.AddAsync(product, cancellationToken);

        if (request.OpeningStock > 0)
        {
            var openingMovement = StockMovement.Post(
                product,
                product.Id,
                StockMovementType.OpeningBalance,
                request.OpeningStock,
                "Opening balance recorded when the product was created",
                $"PRODUCT:{product.Id}",
                _currentUser.RequireUserId());

            await _unitOfWork.StockMovements.AddAsync(openingMovement, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return product.ToDto();
    }
}
