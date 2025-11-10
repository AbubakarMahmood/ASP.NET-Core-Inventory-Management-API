using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Commands.Products;

/// <summary>
/// Update product command handler
/// </summary>
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand, ProductDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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

        // Update properties
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

        // Note: Optimistic concurrency is handled automatically by EF Core
        // using PostgreSQL's xmin (configured in ProductConfiguration)
        // When SaveChangesAsync is called, EF Core will detect concurrent modifications

        try
        {
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ValidationException("RowVersion", "The product has been modified by another user. Please refresh and try again.");
        }

        return _mapper.Map<ProductDto>(product);
    }
}
