using AutoMapper;
using ShoppingListBackend.Api.DTOs.Common;
using ShoppingListBackend.Api.DTOs.Device;
using ShoppingListBackend.Api.DTOs.ShoppingList;
using ShoppingListBackend.Api.Models;

namespace ShoppingListBackend.Api.Mappers;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Device mappings
        CreateMap<Device, DeviceInfo>();
        CreateMap<Device, FriendDto>();

        // ShoppingListItem
        CreateMap<ShoppingListItem, ShoppingListItemDto>()
            .ForMember(dest => dest.CategoryId,
                opt => opt.MapFrom(src => src.ShoppingListCategoryId));

        // ShoppingListCategory
        CreateMap<ShoppingListCategory, ShoppingListCategoryDto>()
            .ForMember(dest => dest.Items,
                opt => opt.MapFrom(src => src.Items.OrderBy(i => i.Position)));

        // ShoppingList – full hydrated DTO
        CreateMap<ShoppingList, ShoppingListDto>()
            .ForMember(dest => dest.Owner,
                opt => opt.MapFrom(src => src.Owner))
            .ForMember(dest => dest.Editors,
                opt => opt.MapFrom(src => src.Editors))
            .ForMember(dest => dest.Categories,
                opt => opt.MapFrom(src => src.Categories.OrderBy(c => c.Position)))
            .ForMember(dest => dest.UpdatedAt,
                opt => opt.MapFrom(src => src.UpdatedAt ?? src.CreatedAt));

        // If you need to map directly from ShoppingList to DeviceShoppingListHeader
        CreateMap<ShoppingList, DeviceShoppingListHeader>()
            .ForMember(dest => dest.Owner,
                opt => opt.MapFrom(src => src.Owner))
            .ForMember(dest => dest.Editors,
                opt => opt.MapFrom(src => src.Editors))
            .ForMember(dest => dest.UpdatedAt,
                opt => opt.MapFrom(src => src.UpdatedAt ?? src.CreatedAt));
    }
}