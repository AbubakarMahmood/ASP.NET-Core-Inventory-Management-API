using AutoMapper;
using InventoryAPI.Application.Commands.Products;
using InventoryAPI.Application.Commands.StockMovements;
using InventoryAPI.Application.DTOs;
using InventoryAPI.Domain.Entities;

namespace InventoryAPI.Application.Mappings;

/// <summary>
/// AutoMapper profile for entity to DTO mappings
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Product mappings
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.IsLowStock, opt => opt.MapFrom(src => src.IsLowStock()));

        CreateMap<CreateProductCommand, Product>();

        // StockMovement mappings
        CreateMap<StockMovement, StockMovementDto>()
            .ForMember(dest => dest.ProductSKU, opt => opt.Ignore())
            .ForMember(dest => dest.ProductName, opt => opt.Ignore())
            .ForMember(dest => dest.PerformedByName, opt => opt.Ignore())
            .ForMember(dest => dest.WorkOrderNumber, opt => opt.Ignore());

        CreateMap<RecordStockMovementCommand, StockMovement>();
    }
}
