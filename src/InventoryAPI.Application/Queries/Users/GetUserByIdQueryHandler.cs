using InventoryAPI.Application.Interfaces;
using AutoMapper;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace InventoryAPI.Application.Queries.Users;

/// <summary>
/// Handler for getting a user by ID
/// </summary>
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetUserByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
        {
            return null;
        }

        return _mapper.Map<UserDto>(user);
    }
}
