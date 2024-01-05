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
        CreateMap<Post, ReadPostResponse>();
        CreateMap<CreatePostRequest, Post>();
        CreateMap<UpdatePostRequest, Post>();
        CreateMap<UpdatePostRequest, Post>().ReverseMap();
    }
}
