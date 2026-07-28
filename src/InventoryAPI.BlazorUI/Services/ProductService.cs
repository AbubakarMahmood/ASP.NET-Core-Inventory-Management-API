using InventoryAPI.BlazorUI.Models;

namespace InventoryAPI.BlazorUI.Services;

public class ProductService
{
    private readonly ApiClient _apiClient;

    public ProductService(ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<PaginatedResult<ProductDto>?> GetProductsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? category = null,
        string? searchTerm = null,
        bool? lowStockOnly = null)
    {
        var queryParams = new List<string>
        {
            $"pageNumber={pageNumber}",
            $"pageSize={pageSize}"
        };

        if (!string.IsNullOrEmpty(category))
            queryParams.Add($"category={Uri.EscapeDataString(category)}");
        if (!string.IsNullOrEmpty(searchTerm))
            queryParams.Add($"searchTerm={Uri.EscapeDataString(searchTerm)}");
        if (lowStockOnly.HasValue)
            queryParams.Add($"lowStockOnly={lowStockOnly.Value}");

        return await _apiClient.GetAsync<PaginatedResult<ProductDto>>(
            $"/api/v1/products?{string.Join("&", queryParams)}");
    }

    public Task<ProductDto?> GetProductByIdAsync(Guid id) =>
        _apiClient.GetAsync<ProductDto>($"/api/v1/products/{id}");

    public Task<ProductDto?> CreateProductAsync(CreateProductRequest request) =>
        _apiClient.PostAsync<CreateProductRequest, ProductDto>("/api/v1/products", request);

    public Task<ProductDto?> UpdateProductAsync(Guid id, UpdateProductRequest request) =>
        _apiClient.PutAsync<UpdateProductRequest, ProductDto>($"/api/v1/products/{id}", request);

    public async Task<bool> DeleteProductAsync(Guid id)
    {
        var response = await _apiClient.DeleteAsync($"/api/v1/products/{id}");
        return response.IsSuccessStatusCode;
    }
}

public class CreateProductRequest
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int OpeningStock { get; set; }
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = string.Empty;
}

public class UpdateProductRequest
{
    public string SKU { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int ReorderPoint { get; set; }
    public int ReorderQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
    public string Location { get; set; } = string.Empty;
    public uint Version { get; set; }
}
