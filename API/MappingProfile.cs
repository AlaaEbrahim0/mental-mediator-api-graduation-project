namespace API;
using AutoMapper;
using Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shared;

public class MappingProfile: Profile
{
    public MappingProfile()
    {
        CreateMap<RegistrationModel, AppUser>();
    }
}
