using Asp.Versioning;
using InventoryAPI.Application.Commands.Products;
using InventoryAPI.Application.Common;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Application.Interfaces;
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
    private readonly IExcelExportService _excelExportService;
    private readonly IPdfExportService _pdfExportService;

    public ProductsController(IMediator mediator, ILogger<ProductsController> logger, IExcelExportService excelExportService, IPdfExportService pdfExportService)
    {
        _mediator = mediator;
        _logger = logger;
        _excelExportService = excelExportService;
        _pdfExportService = pdfExportService;
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

        var query = new GetProductByIdQuery { Id = id };
        var result = await _mediator.Send(query);

        return Ok(result);
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
    public async Task<ActionResult<ProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
        {
            return BadRequest("ID in URL does not match ID in request body");
        }

        _logger.LogInformation("Updating product with ID: {Id}", id);

        var result = await _mediator.Send(command);

        _logger.LogInformation("Product updated successfully with ID: {Id}", result.Id);

        return Ok(result);
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

        var command = new DeleteProductCommand { Id = id };
        await _mediator.Send(command);

        _logger.LogInformation("Product deleted successfully with ID: {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Export products to Excel
    /// </summary>
    /// <param name="category">Filter by category</param>
    /// <param name="searchTerm">Search in name, SKU, or description</param>
    /// <param name="lowStockOnly">Show only low stock items</param>
    /// <returns>Excel file</returns>
    /// <response code="200">Excel file generated successfully</response>
    [HttpGet("export")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportProducts(
        [FromQuery] string? category = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? lowStockOnly = null)
    {
        _logger.LogInformation("Exporting products to Excel");

        // Fetch all products without pagination
        var query = new GetProductsQuery
        {
            PageNumber = 1,
            PageSize = int.MaxValue,
            Category = category,
            SearchTerm = searchTerm,
            LowStockOnly = lowStockOnly
        };

        var result = await _mediator.Send(query);

        // Generate Excel file
        var excelData = _excelExportService.ExportToExcel(result.Items, "Products");

        _logger.LogInformation("Exported {Count} products to Excel", result.Items.Count);

        return File(excelData, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Products_{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    /// <summary>
    /// Export products to PDF
    /// </summary>
    /// <param name="category">Filter by category</param>
    /// <param name="searchTerm">Search in name, SKU, or description</param>
    /// <param name="lowStockOnly">Show only low stock items</param>
    /// <returns>PDF file</returns>
    /// <response code="200">PDF file generated successfully</response>
    [HttpGet("export/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExportProductsToPdf(
        [FromQuery] string? category = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? lowStockOnly = null)
    {
        _logger.LogInformation("Exporting products to PDF");

        // Fetch all products without pagination
        var query = new GetProductsQuery
        {
            PageNumber = 1,
            PageSize = int.MaxValue,
            Category = category,
            SearchTerm = searchTerm,
            LowStockOnly = lowStockOnly
        };

        var result = await _mediator.Send(query);

        // Generate PDF file
        var pdfData = _pdfExportService.ExportToPdf(result.Items, "Products Report");

        _logger.LogInformation("Exported {Count} products to PDF", result.Items.Count);

        return File(pdfData, "application/pdf",
            $"Products_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf");
    }
}
