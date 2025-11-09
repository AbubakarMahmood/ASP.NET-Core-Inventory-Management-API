using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Entities;
using InventoryAPI.Domain.Enums;
using InventoryAPI.Infrastructure.Interfaces;
using MediatR;

namespace InventoryAPI.Application.Commands.WorkOrders;

/// <summary>
/// Handler for submitting a work order
/// </summary>
public class SubmitWorkOrderCommandHandler : IRequestHandler<SubmitWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public SubmitWorkOrderCommandHandler(IUnitOfWork unitOfWork, IMapper _mapper)
    {
        _unitOfWork = unitOfWork;
        this._mapper = _mapper;
    }

    public async Task<WorkOrderDto> Handle(SubmitWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Work order {request.WorkOrderId} not found");

        workOrder.Submit();

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDto(workOrder, cancellationToken);
    }

    private async Task<WorkOrderDto> MapToDto(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<WorkOrderDto>(workOrder);

        var requestedBy = await _unitOfWork.Users.GetByIdAsync(workOrder.RequestedById, cancellationToken);
        dto.RequestedByName = requestedBy?.FullName ?? "Unknown";
        dto.RequestedByEmail = requestedBy?.Email ?? "";

        if (workOrder.AssignedToId.HasValue)
        {
            var assignedTo = await _unitOfWork.Users.GetByIdAsync(workOrder.AssignedToId.Value, cancellationToken);
            dto.AssignedToName = assignedTo?.FullName;
            dto.AssignedToEmail = assignedTo?.Email;
        }

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

/// <summary>
/// Handler for approving a work order
/// </summary>
public class ApproveWorkOrderCommandHandler : IRequestHandler<ApproveWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ApproveWorkOrderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<WorkOrderDto> Handle(ApproveWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Work order {request.WorkOrderId} not found");

        // Validate assigned user exists
        var assignedUser = await _unitOfWork.Users.GetByIdAsync(request.AssignedToId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.AssignedToId} not found");

        workOrder.Approve(request.AssignedToId);

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDto(workOrder, cancellationToken);
    }

    private async Task<WorkOrderDto> MapToDto(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<WorkOrderDto>(workOrder);

        var requestedBy = await _unitOfWork.Users.GetByIdAsync(workOrder.RequestedById, cancellationToken);
        dto.RequestedByName = requestedBy?.FullName ?? "Unknown";
        dto.RequestedByEmail = requestedBy?.Email ?? "";

        if (workOrder.AssignedToId.HasValue)
        {
            var assignedTo = await _unitOfWork.Users.GetByIdAsync(workOrder.AssignedToId.Value, cancellationToken);
            dto.AssignedToName = assignedTo?.FullName;
            dto.AssignedToEmail = assignedTo?.Email;
        }

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

/// <summary>
/// Handler for rejecting a work order
/// </summary>
public class RejectWorkOrderCommandHandler : IRequestHandler<RejectWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public RejectWorkOrderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<WorkOrderDto> Handle(RejectWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Work order {request.WorkOrderId} not found");

        workOrder.Reject(request.Reason);

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDto(workOrder, cancellationToken);
    }

    private async Task<WorkOrderDto> MapToDto(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<WorkOrderDto>(workOrder);

        var requestedBy = await _unitOfWork.Users.GetByIdAsync(workOrder.RequestedById, cancellationToken);
        dto.RequestedByName = requestedBy?.FullName ?? "Unknown";
        dto.RequestedByEmail = requestedBy?.Email ?? "";

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

/// <summary>
/// Handler for starting a work order
/// </summary>
public class StartWorkOrderCommandHandler : IRequestHandler<StartWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public StartWorkOrderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<WorkOrderDto> Handle(StartWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Work order {request.WorkOrderId} not found");

        workOrder.Start();

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDto(workOrder, cancellationToken);
    }

    private async Task<WorkOrderDto> MapToDto(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<WorkOrderDto>(workOrder);

        var requestedBy = await _unitOfWork.Users.GetByIdAsync(workOrder.RequestedById, cancellationToken);
        dto.RequestedByName = requestedBy?.FullName ?? "Unknown";
        dto.RequestedByEmail = requestedBy?.Email ?? "";

        if (workOrder.AssignedToId.HasValue)
        {
            var assignedTo = await _unitOfWork.Users.GetByIdAsync(workOrder.AssignedToId.Value, cancellationToken);
            dto.AssignedToName = assignedTo?.FullName;
            dto.AssignedToEmail = assignedTo?.Email;
        }

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

/// <summary>
/// Handler for completing a work order
/// </summary>
public class CompleteWorkOrderCommandHandler : IRequestHandler<CompleteWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CompleteWorkOrderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<WorkOrderDto> Handle(CompleteWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Work order {request.WorkOrderId} not found");

        workOrder.Complete();

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDto(workOrder, cancellationToken);
    }

    private async Task<WorkOrderDto> MapToDto(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<WorkOrderDto>(workOrder);

        var requestedBy = await _unitOfWork.Users.GetByIdAsync(workOrder.RequestedById, cancellationToken);
        dto.RequestedByName = requestedBy?.FullName ?? "Unknown";
        dto.RequestedByEmail = requestedBy?.Email ?? "";

        if (workOrder.AssignedToId.HasValue)
        {
            var assignedTo = await _unitOfWork.Users.GetByIdAsync(workOrder.AssignedToId.Value, cancellationToken);
            dto.AssignedToName = assignedTo?.FullName;
            dto.AssignedToEmail = assignedTo?.Email;
        }

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

/// <summary>
/// Handler for cancelling a work order
/// </summary>
public class CancelWorkOrderCommandHandler : IRequestHandler<CancelWorkOrderCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CancelWorkOrderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<WorkOrderDto> Handle(CancelWorkOrderCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Work order {request.WorkOrderId} not found");

        workOrder.Cancel();

        _unitOfWork.WorkOrders.Update(workOrder);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return await MapToDto(workOrder, cancellationToken);
    }

    private async Task<WorkOrderDto> MapToDto(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<WorkOrderDto>(workOrder);

        var requestedBy = await _unitOfWork.Users.GetByIdAsync(workOrder.RequestedById, cancellationToken);
        dto.RequestedByName = requestedBy?.FullName ?? "Unknown";
        dto.RequestedByEmail = requestedBy?.Email ?? "";

        if (workOrder.AssignedToId.HasValue)
        {
            var assignedTo = await _unitOfWork.Users.GetByIdAsync(workOrder.AssignedToId.Value, cancellationToken);
            dto.AssignedToName = assignedTo?.FullName;
            dto.AssignedToEmail = assignedTo?.Email;
        }

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

/// <summary>
/// Handler for issuing items from a work order (creates stock movements)
/// </summary>
public class IssueWorkOrderItemsCommandHandler : IRequestHandler<IssueWorkOrderItemsCommand, WorkOrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IMediator _mediator;

    public IssueWorkOrderItemsCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IMediator mediator)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _mediator = mediator;
    }

    public async Task<WorkOrderDto> Handle(IssueWorkOrderItemsCommand request, CancellationToken cancellationToken)
    {
        var workOrder = await _unitOfWork.WorkOrders.GetByIdWithDetailsAsync(request.WorkOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Work order {request.WorkOrderId} not found");

        if (workOrder.Status != WorkOrderStatus.InProgress)
        {
            throw new InvalidOperationException("Only in-progress work orders can have items issued");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var issueRequest in request.Items)
            {
                // Find the work order item
                var item = workOrder.Items.FirstOrDefault(i => i.ProductId == issueRequest.ProductId)
                    ?? throw new KeyNotFoundException($"Product {issueRequest.ProductId} not found in work order");

                // Validate quantity
                if (item.QuantityIssued + issueRequest.Quantity > item.QuantityRequested)
                {
                    throw new InvalidOperationException(
                        $"Cannot issue {issueRequest.Quantity} units. Remaining: {item.QuantityRequested - item.QuantityIssued}");
                }

                // Create stock movement (Issue type)
                var stockMovementCommand = new StockMovements.RecordStockMovementCommand
                {
                    ProductId = issueRequest.ProductId,
                    Type = StockMovementType.Issue,
                    Quantity = issueRequest.Quantity,
                    FromLocation = issueRequest.FromLocation,
                    Reason = $"Issued for Work Order {workOrder.OrderNumber}",
                    ReferenceNumber = workOrder.OrderNumber,
                    WorkOrderId = workOrder.Id
                };

                await _mediator.Send(stockMovementCommand, cancellationToken);

                // Update quantity issued
                item.QuantityIssued += issueRequest.Quantity;
            }

            _unitOfWork.WorkOrders.Update(workOrder);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return await MapToDto(workOrder, cancellationToken);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private async Task<WorkOrderDto> MapToDto(WorkOrder workOrder, CancellationToken cancellationToken)
    {
        var dto = _mapper.Map<WorkOrderDto>(workOrder);

        var requestedBy = await _unitOfWork.Users.GetByIdAsync(workOrder.RequestedById, cancellationToken);
        dto.RequestedByName = requestedBy?.FullName ?? "Unknown";
        dto.RequestedByEmail = requestedBy?.Email ?? "";

        if (workOrder.AssignedToId.HasValue)
        {
            var assignedTo = await _unitOfWork.Users.GetByIdAsync(workOrder.AssignedToId.Value, cancellationToken);
            dto.AssignedToName = assignedTo?.FullName;
            dto.AssignedToEmail = assignedTo?.Email;
        }

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
