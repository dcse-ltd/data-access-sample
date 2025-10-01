using AutoMapper;
using DataAccess.Entity.Models;
using DataAccess.Services.Interfaces;
using Features.Customer.Dtos;

namespace Features.Customer.Mapping;

public class CustomerMappingProfile : Profile
{
    public CustomerMappingProfile(ICurrentUserService currentUserService)
    {
        CreateMap<DataAccess.Entity.Customer, CustomerDto>()
            .ForMember(
                dest => dest.Locked,
                opt => opt.MapFrom(src => src.LockInfo.LockedByUserId != null))
            .ForMember(
                dest => dest.LockedBy,
                opt => opt.MapFrom(src =>
                    src.LockInfo.LockedByUserId.HasValue
                        ? currentUserService.GetUserName(src.LockInfo.LockedByUserId.Value)
                        : null))
            .ForMember(dest => dest.CreatedOn, opt => opt.MapFrom(src => src.AuditInfo.CreatedOn))
            .ForMember(dest => dest.CreatedBy, opt => opt.MapFrom(src => src.AuditInfo.CreatedBy))
            .ForMember(dest => dest.ModifiedOn, opt => opt.MapFrom(src => src.AuditInfo.ModifiedOn))
            .ForMember(dest => dest.ModifiedBy, opt => opt.MapFrom(src => src.AuditInfo.ModifiedBy));

        CreateMap<CustomerDto, DataAccess.Entity.Customer>()
            .ForMember(dest => dest.AuditInfo, opt => opt.Ignore())
            .ForMember(dest => dest.LockInfo,opt => opt.Ignore());
    }
}