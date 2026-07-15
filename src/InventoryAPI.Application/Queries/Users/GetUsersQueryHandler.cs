using InventoryAPI.Application.Interfaces;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;

using InventoryAPI.Application.Mappings;

namespace InventoryAPI.Application.Queries.Users;

/// <summary>
/// Handler for getting users with filtering
/// </summary>
public class GetUsersQueryHandler : IRequestHandler<GetUsersQuery, PaginatedResult<UserDto>>
{
    private readonly IApplicationDbContext _context;

    public GetUsersQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Users.AsQueryable();

        // Apply filters
        if (request.Role.HasValue)
        {
            query = query.Where(u => u.Role == request.Role.Value);
        }

        if (request.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == request.IsActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchLower = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(searchLower) ||
                u.FirstName.ToLower().Contains(searchLower) ||
                u.LastName.ToLower().Contains(searchLower));
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination and ordering
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        // Map to DTOs
        var userDtos = users.Select(x => x.ToDto()).ToList();

        return new PaginatedResult<UserDto>(
            userDtos,
            totalCount,
            request.PageNumber,
            request.PageSize
        );
    }
}
