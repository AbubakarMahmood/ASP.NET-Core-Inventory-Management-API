using InventoryAPI.Application.Commands.Auth;
using InventoryAPI.Application.DTOs;
using MediatR;
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
        _logger.LogInformation("Login attempt for user: {Email}", request.Email);

        var command = new LoginCommand
        {
            Email = request.Email,
            Password = request.Password
        };

        var result = await _mediator.Send(command);

        _logger.LogInformation("Login successful for user: {Email}", request.Email);

        return Ok(result);
    }

    /// <summary>
    /// Refresh access token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>New JWT tokens</returns>
    /// <response code="200">Token refreshed</response>
    /// <response code="401">Invalid refresh token</response>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] string refreshToken)
    {
        _logger.LogInformation("Token refresh attempt");

        // TODO: Implement refresh token logic
        return StatusCode(StatusCodes.Status501NotImplemented, "Refresh token endpoint not yet implemented");
    }
}
