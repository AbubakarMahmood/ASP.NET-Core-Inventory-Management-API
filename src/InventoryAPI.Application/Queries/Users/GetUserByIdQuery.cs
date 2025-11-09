using InventoryAPI.Application.DTOs;
using MediatR;

namespace InventoryAPI.Application.Queries.Users;

/// <summary>
/// Query to get a single user by ID
/// </summary>
public class GetUserByIdQuery : IRequest<UserDto?>
{
    public Guid UserId { get; set; }

    public GetUserByIdQuery(Guid userId)
    {
        UserId = userId;
    }
}
