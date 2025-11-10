using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace InventoryAPI.Application.Commands.WorkOrders;

/// <summary>
/// Handler for creating a new work order
/// </summary>
public class CreateWorkOrderCommandHandler : IRequestHandler<CreateWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CreateWorkOrderCommandHandler(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IHttpContextAccessor httpContextAccessor)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<WorkOrderDto> Handle(CreateWorkOrderCommand request, CancellationToken cancellationToken)
    {
        // Get current user ID from claims
        var userIdClaim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var currentUserId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        // Validate products exist
        foreach (var item in request.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
            if (product == null)
            {
                throw new KeyNotFoundException($"Product with ID {item.ProductId} not found");
            }
        }

        // Generate order number
        var orderNumber = await GenerateOrderNumberAsync(cancellationToken);

        // Create work order entity
        var workOrder = new WorkOrder
        {
            OrderNumber = orderNumber,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            DueDate = request.DueDate,
            RequestedById = currentUserId,
            Status = Domain.Enums.WorkOrderStatus.Draft
        };

        // Add items
        foreach (var itemRequest in request.Items)
        {
            workOrder.Items.Add(new WorkOrderItem
            {
                ProductId = itemRequest.ProductId,
                QuantityRequested = itemRequest.QuantityRequested,
                QuantityIssued = 0,
                Notes = itemRequest.Notes
            });
        }

        // Save to database
        await _unitOfWork.WorkOrders.AddAsync(workOrder, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Load related data for DTO
        var savedWorkOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(workOrder.Id, cancellationToken);

        return await MapToDto(savedWorkOrder!, cancellationToken);
    }

    private async Task<string> GenerateOrderNumberAsync(CancellationToken cancellationToken)
    {
        // Get count of work orders to generate sequential number
        var count = await _unitOfWork.WorkOrders.CountAsync(cancellationToken);
        var orderNumber = $"WO-{DateTime.UtcNow:yyyyMMdd}-{(count + 1):D4}";
        return orderNumber;
    }

    private async Task<WorkOrderDto> MapToDto(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<WorkOrderDto>(workOrder);

        // Map user names
        var requestedBy = await _unitOfWork.Users.GetByIdAsync(workOrder.RequestedById, cancellationToken);
        dto.RequestedByName = requestedBy?.FullName ?? "Unknown";
        dto.RequestedByEmail = requestedBy?.Email ?? "";

        if (workOrder.AssignedToId.HasValue)
        {
            var assignedTo = await _unitOfWork.Users.GetByIdAsync(workOrder.AssignedToId.Value, cancellationToken);
            dto.AssignedToName = assignedTo?.FullName;
            dto.AssignedToEmail = assignedTo?.Email;
        }

        // Map items with product details
        dto.Items = new List<WorkOrderItemDto>();
        foreach (var item in workOrder.Items)
        {
            var product = await _unitOfWork.Products.GetByIdAsync(item.ProductId, cancellationToken);
            dto.Items.Add(new WorkOrderItemDto
            {
                Id = item.Id,
                WorkOrderId = item.WorkOrderId,
                ProductId = item.ProductId,
                ProductSKU = product?.SKU ?? "",
                ProductName = product?.Name ?? "",
                UnitOfMeasure = product?.UnitOfMeasure ?? "",
                CurrentStock = product?.CurrentStock ?? 0,
                QuantityRequested = item.QuantityRequested,
                QuantityIssued = item.QuantityIssued,
                Notes = item.Notes
            });
        }

        return dto;
    }
}
