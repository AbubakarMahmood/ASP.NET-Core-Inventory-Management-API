using Asp.Versioning;
using InventoryAPI.Application.Commands.Auth;
using InventoryAPI.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Api.Controllers;

/// <summary>
/// Authentication endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IMediator mediator, ILogger<AuthController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Authenticate user and get access token
    /// </summary>
    /// <param name="request">Login credentials</param>
    /// <returns>JWT tokens</returns>
    /// <response code="200">Login successful</response>
    /// <response code="400">Invalid credentials</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var result = await _mediator.Send(command);

        _logger.LogInformation("Login successful for {Email}", request.Email);

        return Ok(result);
    }

    /// <summary>
    /// Exchange a refresh token for a new token pair
    /// </summary>
    /// <param name="request">The refresh token issued at login</param>
    /// <returns>New JWT tokens</returns>
    /// <response code="200">Token refreshed</response>
    /// <response code="401">Invalid or expired refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var command = new RefreshTokenCommand { RefreshToken = request.RefreshToken };

        var result = await _mediator.Send(command);

        return Ok(result);
    }

    /// <summary>
    /// Revoke the current user's refresh token
    /// </summary>
    /// <response code="204">Refresh token revoked</response>
    /// <response code="401">Not authenticated</response>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        await _mediator.Send(new RevokeTokenCommand());

        return NoContent();
    }
}

/// <summary>
/// Refresh token request body
/// </summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
