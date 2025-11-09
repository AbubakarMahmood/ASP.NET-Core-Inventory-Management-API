using AutoMapper;
using InventoryAPI.Application.Commands.Products;
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

        // Add more mappings as needed
    }
}
