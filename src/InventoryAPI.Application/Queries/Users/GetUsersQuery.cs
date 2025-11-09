using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Enums;
using MediatR;

namespace InventoryAPI.Application.Queries.Users;

/// <summary>
/// Query to get users with filtering and pagination
/// </summary>
public class GetUsersQuery : IRequest<PaginatedResult<UserDto>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public UserRole? Role { get; set; }
    public bool? IsActive { get; set; }
    public string? SearchTerm { get; set; }
}
