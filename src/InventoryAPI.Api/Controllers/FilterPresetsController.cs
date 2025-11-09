using InventoryAPI.Application.Commands.FilterPresets;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Queries.FilterPresets;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Api.Controllers;

/// <summary>
/// Filter presets management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class FilterPresetsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<FilterPresetsController> _logger;

    public FilterPresetsController(IMediator mediator, ILogger<FilterPresetsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all filter presets for the current user
    /// </summary>
    /// <param name="entityType">Filter by entity type (Product, WorkOrder, AuditLog)</param>
    /// <param name="includeShared">Include shared presets from other users</param>
    /// <returns>List of filter presets</returns>
    /// <response code="200">Filter presets retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(List<FilterPresetDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<List<FilterPresetDto>>> GetFilterPresets(
        [FromQuery] string? entityType = null,
        [FromQuery] bool? includeShared = true)
    {
        _logger.LogInformation("Fetching filter presets for entity type: {EntityType}", entityType ?? "All");

        var query = new GetFilterPresetsQuery
        {
            EntityType = entityType,
            IncludeShared = includeShared
        };

        var result = await _mediator.Send(query);

        _logger.LogInformation("Retrieved {Count} filter presets", result.Count);

        return Ok(result);
    }

    /// <summary>
    /// Get filter preset by ID
    /// </summary>
    /// <param name="id">Filter preset ID</param>
    /// <returns>Filter preset details</returns>
    /// <response code="200">Filter preset found</response>
    /// <response code="404">Filter preset not found</response>
    /// <response code="401">Unauthorized access</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FilterPresetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FilterPresetDto>> GetFilterPreset(Guid id)
    {
        _logger.LogInformation("Fetching filter preset with ID: {Id}", id);

        var query = new GetFilterPresetByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        return Ok(result);
    }

    /// <summary>
    /// Create a new filter preset
    /// </summary>
    /// <param name="command">Filter preset details</param>
    /// <returns>Created filter preset</returns>
    /// <response code="201">Filter preset created successfully</response>
    /// <response code="400">Invalid filter preset data</response>
    [HttpPost]
    [ProducesResponseType(typeof(FilterPresetDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FilterPresetDto>> CreateFilterPreset([FromBody] CreateFilterPresetCommand command)
    {
        _logger.LogInformation("Creating filter preset: {Name} for entity type: {EntityType}",
            command.Name, command.EntityType);

        var result = await _mediator.Send(command);

        _logger.LogInformation("Filter preset created successfully with ID: {Id}", result.Id);

        return CreatedAtAction(nameof(GetFilterPreset), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing filter preset
    /// </summary>
    /// <param name="id">Filter preset ID</param>
    /// <param name="command">Updated filter preset details</param>
    /// <returns>Updated filter preset</returns>
    /// <response code="200">Filter preset updated successfully</response>
    /// <response code="400">Invalid filter preset data</response>
    /// <response code="404">Filter preset not found</response>
    /// <response code="401">Unauthorized - can only update own presets</response>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FilterPresetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<FilterPresetDto>> UpdateFilterPreset(Guid id, [FromBody] UpdateFilterPresetCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("ID in URL does not match ID in request body");
        }

        _logger.LogInformation("Updating filter preset with ID: {Id}", id);

        var result = await _mediator.Send(command);

        _logger.LogInformation("Filter preset updated successfully");

        return Ok(result);
    }

    /// <summary>
    /// Delete a filter preset
    /// </summary>
    /// <param name="id">Filter preset ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Filter preset deleted successfully</response>
    /// <response code="404">Filter preset not found</response>
    /// <response code="401">Unauthorized - can only delete own presets</response>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> DeleteFilterPreset(Guid id)
    {
        _logger.LogInformation("Deleting filter preset with ID: {Id}", id);

        var command = new DeleteFilterPresetCommand { Id = id };
        await _mediator.Send(command);

        _logger.LogInformation("Filter preset deleted successfully");

        return NoContent();
    }
}
