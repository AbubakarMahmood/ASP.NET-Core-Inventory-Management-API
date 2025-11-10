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
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public UpdateProductCommandHandler(IUnitOfWork unitOfWork, IApplicationDbContext context, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _context = context;
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

        // Set the row version for optimistic concurrency
        if (request.RowVersion != null && request.RowVersion.Length > 0)
        {
            _context.Entry(product).Property("xmin").OriginalValue = request.RowVersion;
        }

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
