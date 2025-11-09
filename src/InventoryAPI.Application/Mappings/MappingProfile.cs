using AutoMapper;
using InventoryAPI.Application.Commands.Products;
using InventoryAPI.Application.Commands.WorkOrders;
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

        // WorkOrder mappings
        CreateMap<WorkOrder, WorkOrderDto>()
            .ForMember(dest => dest.RequestedByName, opt => opt.Ignore())
            .ForMember(dest => dest.RequestedByEmail, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedToName, opt => opt.Ignore())
            .ForMember(dest => dest.AssignedToEmail, opt => opt.Ignore())
            .ForMember(dest => dest.Items, opt => opt.Ignore());

        CreateMap<WorkOrderItem, WorkOrderItemDto>()
            .ForMember(dest => dest.ProductSKU, opt => opt.Ignore())
            .ForMember(dest => dest.ProductName, opt => opt.Ignore())
            .ForMember(dest => dest.UnitOfMeasure, opt => opt.Ignore())
            .ForMember(dest => dest.CurrentStock, opt => opt.Ignore());

        CreateMap<CreateWorkOrderCommand, WorkOrder>();

        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.FullName));

        // FilterPreset mappings
        CreateMap<FilterPreset, FilterPresetDto>();
    }
}
