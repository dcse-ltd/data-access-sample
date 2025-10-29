using Application.Customer.Dtos;
using AutoMapper;
using Infrastructure.Services.Interfaces;

namespace Application.Customer.Mapping;

public class CustomerMappingProfile : Profile
{
    public CustomerMappingProfile(ICurrentUserService currentUserService)
    {
        CreateMap<Entities.Customer, CustomerDto>()
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
            
            .ForMember(dest => dest.RowVersion, opt => opt.MapFrom(src => src.Concurrency.RowVersion));

        CreateMap<CustomerDto, Entities.Customer>()
            .ForMember(dest => dest.Auditing, opt => opt.Ignore())
            .ForMember(dest => dest.Locking.LockInfo,opt => opt.Ignore())
            .ForMember(dest => dest.Concurrency.RowVersion, opt => opt.MapFrom(src => src.RowVersion));
    }
}