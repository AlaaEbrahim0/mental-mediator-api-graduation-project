namespace API;
using AutoMapper;
using Domain.Entities;
using Infrastructure;
using Shared;

public class MappingProfile: Profile
{
    public MappingProfile()
    {
        CreateMap<RegistrationRequest, AppUser>();
    }
}
