namespace API;
using AutoMapper;
using Infrastructure;
using Shared;

public class MappingProfile: Profile
{
    public MappingProfile()
    {
        CreateMap<RegistrationModel, AppUser>();
    }
}
