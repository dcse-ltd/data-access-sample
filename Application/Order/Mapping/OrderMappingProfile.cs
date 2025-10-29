using Application.Order.Dtos;
using AutoMapper;
using Infrastructure.Services.Interfaces;

namespace Application.Order.Mapping;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile(ICurrentUserService currentUserService)
    {
        CreateMap<Entities.Order, OrderDto>()
            .ForMember(
                dest => dest.Locked,
                opt => opt.MapFrom(src => src.Locking.LockInfo.LockedByUserId != null))
            .ForMember(
                dest => dest.LockedBy,
                opt => opt.MapFrom(src =>
                    src.Locking.LockInfo.LockedByUserId.HasValue
                        ? currentUserService.GetUserName(src.Locking.LockInfo.LockedByUserId.Value)
                        : null))
            .ForMember(dest => dest.LockedByUserId, opt => opt.MapFrom(src => src.Locking.LockInfo.LockedByUserId))
            
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => src.Auditing.AuditInfo.CreatedAtUtc))
            .ForMember(
                dest => dest.CreatedBy, 
                opt => opt.MapFrom(src => currentUserService.GetUserName(src.Auditing.AuditInfo.CreatedByUserId)))
            .ForMember(dest => dest.CreatedByUserId, opt => opt.MapFrom(src => src.Auditing.AuditInfo.CreatedByUserId))
            
            .ForMember(dest => dest.ModifiedAtUtc, opt => opt.MapFrom(src => src.Auditing.AuditInfo.ModifiedAtUtc))
            .ForMember(dest => dest.ModifiedBy, opt => opt.MapFrom(src => currentUserService.GetUserName(src.Auditing.AuditInfo.ModifiedByUserId)))
            .ForMember(dest => dest.ModifiedByUserId, opt => opt.MapFrom(src => src.Auditing.AuditInfo.ModifiedByUserId))
            
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.Concurrency.RowVersion))
            
            .ForMember(dest => dest.CustomerFirstName, opt => opt.MapFrom(src => src.Customer.FirstName))
            .ForMember(dest => dest.CustomerLastName, opt => opt.MapFrom(src => src.Customer.LastName))
            .ForMember(dest => dest.CustomerEmail, opt => opt.MapFrom(src => src.Customer.Email))
            .ForMember(dest => dest.CustomerPhone, opt => opt.MapFrom(src => src.Customer.Phone))
            
            .ForMember(dest => dest.OrderProducts, opt => opt.MapFrom(src => src.OrderProducts));

        CreateMap<OrderDto, Entities.Order>()
            .ForMember(dest => dest.Auditing, opt => opt.Ignore())
            .ForMember(dest => dest.Locking, opt => opt.Ignore())
            .ForMember(dest => dest.Concurrency.RowVersion, opt => opt.MapFrom(src => src.RowVersion))
            .ForMember(dest => dest.Customer, opt => opt.Ignore())
            .ForMember(dest => dest.OrderProducts, opt => opt.Ignore());

        CreateMap<Entities.OrderProduct, OrderProductDto>()
            .ForMember(
                dest => dest.Locked,
                opt => opt.MapFrom(src => src.Locking.LockInfo.LockedByUserId != null))
            .ForMember(
                dest => dest.LockedBy,
                opt => opt.MapFrom(src =>
                    src.Locking.LockInfo.LockedByUserId.HasValue
                        ? currentUserService.GetUserName(src.Locking.LockInfo.LockedByUserId.Value)
                        : null))
            .ForMember(dest => dest.LockedByUserId, opt => opt.MapFrom(src => src.Locking.LockInfo.LockedByUserId))
            
            .ForMember(dest => dest.CreatedAtUtc, opt => opt.MapFrom(src => src.Auditing.AuditInfo.CreatedAtUtc))
            .ForMember(
                dest => dest.CreatedBy, 
                opt => opt.MapFrom(src => currentUserService.GetUserName(src.Auditing.AuditInfo.CreatedByUserId)))
            .ForMember(dest => dest.CreatedByUserId, opt => opt.MapFrom(src => src.Auditing.AuditInfo.CreatedByUserId))
            
            .ForMember(dest => dest.ModifiedAtUtc, opt => opt.MapFrom(src => src.Auditing.AuditInfo.ModifiedAtUtc))
            .ForMember(dest => dest.ModifiedBy, opt => opt.MapFrom(src => currentUserService.GetUserName(src.Auditing.AuditInfo.ModifiedByUserId)))
            .ForMember(dest => dest.ModifiedByUserId, opt => opt.MapFrom(src => src.Auditing.AuditInfo.ModifiedByUserId))
            
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.Concurrency.RowVersion))
            
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
            .ForMember(dest => dest.ProductDescription, opt => opt.MapFrom(src => src.Product.Description));

        CreateMap<OrderProductDto, Entities.OrderProduct>()
            .ForMember(dest => dest.Auditing, opt => opt.Ignore())
            .ForMember(dest => dest.Locking, opt => opt.Ignore())
            .ForMember(dest => dest.Concurrency.RowVersion, opt => opt.MapFrom(src => src.RowVersion))
            .ForMember(dest => dest.Product, opt => opt.Ignore())
            .ForMember(dest => dest.Order, opt => opt.Ignore())
            .ForMember(dest => dest.OrderId, opt => opt.Ignore());
    }
}