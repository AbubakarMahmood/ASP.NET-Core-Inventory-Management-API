using InventoryAPI.Application.Commands.Products;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Queries.Products;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Api.Controllers;

/// <summary>
/// Products management endpoints
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get all products with pagination and filtering
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    /// <param name="category">Filter by category</param>
    /// <param name="searchTerm">Search in name, SKU, or description</param>
    /// <param name="lowStockOnly">Show only low stock items</param>
    /// <returns>Paginated list of products</returns>
    /// <response code="200">Products retrieved successfully</response>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResult<ProductDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PaginatedResult<ProductDto>>> GetProducts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? category = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? lowStockOnly = null)
    {
        var query = new GetProductsQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Category = category,
            SearchTerm = searchTerm,
            LowStockOnly = lowStockOnly
        };

        var result = await _mediator.Send(query);

        _logger.LogInformation("Retrieved {Count} products (page {Page} of {Total})",
            result.Items.Count, result.PageNumber, result.TotalPages);

        return Ok(result);
    }

    /// <summary>
    /// Create a new product
    /// </summary>
    /// <param name="command">Product details</param>
    /// <returns>Created product</returns>
    /// <response code="201">Product created successfully</response>
    /// <response code="400">Invalid product data</response>
    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ProductDto>> CreateProduct([FromBody] CreateProductCommand command)
    {
        _logger.LogInformation("Creating product with SKU: {SKU}", command.SKU);

        var result = await _mediator.Send(command);

        _logger.LogInformation("Product created successfully with ID: {Id}", result.Id);

        return CreatedAtAction(nameof(GetProduct), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    /// <response code="200">Product found</response>
    /// <response code="404">Product not found</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        _logger.LogInformation("Retrieving product with ID: {Id}", id);

        // TODO: Implement GetProductByIdQuery
        return StatusCode(StatusCodes.Status501NotImplemented, "Get product by ID not yet implemented");
    }

    /// <summary>
    /// Update existing product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="command">Updated product details</param>
    /// <returns>Updated product</returns>
    /// <response code="200">Product updated successfully</response>
    /// <response code="404">Product not found</response>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] CreateProductCommand command)
    {
        _logger.LogInformation("Updating product with ID: {Id}", id);

        // TODO: Implement UpdateProductCommand
        return StatusCode(StatusCodes.Status501NotImplemented, "Update product not yet implemented");
    }

    /// <summary>
    /// Delete product (soft delete)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Product deleted successfully</response>
    /// <response code="404">Product not found</response>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        _logger.LogInformation("Deleting product with ID: {Id}", id);

        // TODO: Implement DeleteProductCommand
        return StatusCode(StatusCodes.Status501NotImplemented, "Delete product not yet implemented");
    }
}
