using MediatR;

namespace InventoryAPI.Application.Commands.Products;

/// <summary>
/// Delete product command (soft delete)
/// </summary>
public class DeleteProductCommand : IRequest<Unit>
{
    public Guid Id { get; set; }
}
