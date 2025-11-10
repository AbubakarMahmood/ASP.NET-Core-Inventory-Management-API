using InventoryAPI.Application.Common;
using Asp.Versioning;
using InventoryAPI.Application.Commands.Users;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Queries.Users;
using InventoryAPI.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Api.Controllers;

/// <summary>
/// User management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IMediator mediator, ILogger<UsersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all users with optional filtering
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PaginatedResult<UserDto>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching users: Page {PageNumber}, Size {PageSize}", pageNumber, pageSize);

        var query = new GetUsersQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Role = role,
            IsActive = isActive,
            SearchTerm = searchTerm
        };

        var result = await _mediator.Send(query, cancellationToken);

        _logger.LogInformation("Retrieved {Count} users out of {TotalCount}", result.Items.Count, result.TotalCount);

        return Ok(result);
    }

    /// <summary>
    /// Get a single user by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> GetUser(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching user {UserId}", id);

        var query = new GetUserByIdQuery(id);
        var result = await _mediator.Send(query, cancellationToken);

        if (result == null)
        {
            _logger.LogWarning("User {UserId} not found", id);
            return NotFound(new { message = $"User {id} not found" });
        }

        return Ok(result);
    }

    /// <summary>
    /// Create a new user
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> CreateUser(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating new user: {Email}", command.Email);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("User created: {UserId}", result.Id);

        return CreatedAtAction(nameof(GetUser), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing user
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (id != command.UserId)
        {
            return BadRequest(new { message = "User ID mismatch" });
        }

        _logger.LogInformation("Updating user {UserId}", id);

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("User {UserId} updated", id);

        return Ok(result);
    }

    /// <summary>
    /// Change user password
    /// </summary>
    [HttpPost("{id:guid}/change-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> ChangePassword(
        Guid id,
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Changing password for user {UserId}", id);

        var command = new ChangePasswordCommand
        {
            UserId = id,
            NewPassword = request.NewPassword
        };

        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("Password changed for user {UserId}", id);

        return Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    /// Delete a user (soft delete)
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> DeleteUser(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting user {UserId}", id);

        var command = new DeleteUserCommand { UserId = id };
        var result = await _mediator.Send(command, cancellationToken);

        _logger.LogInformation("User {UserId} deleted", id);

        return Ok(new { message = "User deleted successfully" });
    }
}

// Request DTOs
public class ChangePasswordRequest
{
    public string NewPassword { get; set; } = string.Empty;
}
