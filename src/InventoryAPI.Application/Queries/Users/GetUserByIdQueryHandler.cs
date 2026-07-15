using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Queries.Users;

/// <summary>
/// Handler for getting a user by ID
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IApplicationDbContext _context;

    public GetUserByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        return user.ToDto();
    }
}
