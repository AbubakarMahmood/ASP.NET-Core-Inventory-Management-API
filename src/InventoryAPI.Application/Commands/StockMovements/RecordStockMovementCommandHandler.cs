using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Mappings;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Commands.StockMovements;

/// <summary>
/// Records one append-only ledger row and updates the corresponding product
/// balance in the same unit-of-work commit.
/// </summary>
public class RecordStockMovementCommandHandler : IRequestHandler<RecordStockMovementCommand, StockMovementDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RecordStockMovementCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<StockMovementDto> Handle(
        RecordStockMovementCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Type is StockMovementType.Transfer or StockMovementType.OpeningBalance)
        {
            throw new BusinessRuleViolationException(
                request.Type == StockMovementType.Transfer
                    ? "Transfer is not supported by the single-location inventory model. See RFC-0001."
                    : "OpeningBalance can only be created atomically with a new product.");
        }

        var normalizedReason = TextNormalization.Required(request.Reason);
        var normalizedReference = TextNormalization.OptionalOrNull(request.Reference);
        var priorMovements = (await _unitOfWork.StockMovements.FindAsync(
            movement => movement.OperationId == request.OperationId,
            cancellationToken)).ToList();

        if (priorMovements.Count > 0)
        {
            if (priorMovements.Count != 1 || !IsReplayOf(priorMovements[0], request, normalizedReason, normalizedReference))
            {
                throw new IdempotencyConflictException(request.OperationId);
            }

            return await EnrichAsync(priorMovements[0], cancellationToken);
        }

        var product = await _unitOfWork.Products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);

        var userId = _currentUser.RequireUserId();
        var movement = StockMovement.Post(
            product,
            request.OperationId,
            request.Type,
            request.Quantity,
            normalizedReason,
            normalizedReference,
            userId);

        try
        {
            await _unitOfWork.StockMovements.AddAsync(movement, cancellationToken);
            _unitOfWork.Products.Update(product);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException(
                "The product balance changed concurrently. Refresh the product and retry with a new operation id.",
                exception);
        }

        return await EnrichAsync(movement, cancellationToken, product);
    }

    private async Task<StockMovementDto> EnrichAsync(
        StockMovement movement,
        CancellationToken cancellationToken,
        Product? loadedProduct = null)
    {
        var result = movement.ToDto();
        var product = loadedProduct
            ?? await _unitOfWork.Products.GetByIdAsync(movement.ProductId, cancellationToken);
        var user = await _unitOfWork.Users.GetByIdAsync(movement.PerformedById, cancellationToken);

        if (product != null)
        {
            result.ProductSKU = product.SKU;
            result.ProductName = product.Name;
            result.UnitOfMeasure = product.UnitOfMeasure;
        }

        result.PerformedByName = user?.FullName ?? string.Empty;
        return result;
    }

    private static bool IsReplayOf(
        StockMovement movement,
        RecordStockMovementCommand request,
        string normalizedReason,
        string? normalizedReference)
    {
        return movement.WorkOrderId == null
            && movement.ProductId == request.ProductId
            && movement.Type == request.Type
            && movement.Quantity == request.Quantity
            && string.Equals(movement.Reason, normalizedReason, StringComparison.Ordinal)
            && string.Equals(movement.Reference, normalizedReference, StringComparison.Ordinal);
    }
}
