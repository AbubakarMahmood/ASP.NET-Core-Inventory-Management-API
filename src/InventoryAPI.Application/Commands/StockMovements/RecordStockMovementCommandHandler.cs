using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using InventoryAPI.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace InventoryAPI.Application.Commands.StockMovements;

public class RecordStockMovementCommandHandler : IRequestHandler<RecordStockMovementCommand, StockMovementDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RecordStockMovementCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<StockMovementDto> Handle(RecordStockMovementCommand request, CancellationToken cancellationToken)
    {
        // Get the product
        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken);
        if (product == null)
            throw new NotFoundException(nameof(Product), request.ProductId);

        // Get current user ID
        var userId = GetCurrentUserId();

        // Validate stock for issues
        if (request.Type == StockMovementType.Issue && product.CurrentStock < request.Quantity)
        {
            throw new InsufficientStockException(product.Id, product.CurrentStock, request.Quantity);
        }

        // Create stock movement record
        var movement = new StockMovement
        {
            ProductId = request.ProductId,
            Type = request.Type,
            Quantity = request.Quantity,
            SourceLocation = request.SourceLocation,
            DestinationLocation = request.DestinationLocation,
            Reason = request.Reason,
            Reference = request.Reference,
            WorkOrderId = request.WorkOrderId,
            PerformedById = userId,
            Timestamp = DateTime.UtcNow,
            UnitCostAtTransaction = product.UnitCost
        };

        // Update product stock based on movement type
        switch (request.Type)
        {
            case StockMovementType.Receipt:
            case StockMovementType.Return:
                product.AdjustStock(request.Quantity); // Increase
                break;
            case StockMovementType.Issue:
            case StockMovementType.Adjustment when request.Quantity < 0:
                product.AdjustStock(-request.Quantity); // Decrease
                break;
            case StockMovementType.Adjustment when request.Quantity > 0:
                product.AdjustStock(request.Quantity); // Increase
                break;
            case StockMovementType.Transfer:
                // For transfers, just record the movement
                // Location change is handled by SourceLocation/DestinationLocation
                if (!string.IsNullOrEmpty(request.DestinationLocation))
                {
                    product.Location = request.DestinationLocation;
                }
                break;
        }

        // Save everything in a transaction
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            await _unitOfWork.StockMovements.AddAsync(movement, cancellationToken);
            _unitOfWork.Products.Update(product);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }

        // Return DTO with full details
        var result = _mapper.Map<StockMovementDto>(movement);
        result.ProductSKU = product.SKU;
        result.ProductName = product.Name;

        var user = await _unitOfWork.Users.GetByIdAsync(userId, cancellationToken);
        if (user != null)
        {
            result.PerformedByName = user.FullName;
        }

        return result;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (Guid.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }

        throw new UnauthorizedAccessException("User not authenticated");
    }
}
