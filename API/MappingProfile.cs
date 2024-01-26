namespace API;
using AutoMapper;
using Domain.Entities;
using Shared.AuthDtos;
using Shared.CommentsDtos;
using Shared.PostsDto;
using Shared.ReplyDtos;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<RegistrationRequest, AppUser>();

        CreateMap<Post, PostResponse>();
        CreateMap<CreatePostRequest, Post>();
        CreateMap<UpdatePostRequest, Post>().ReverseMap();

        CreateMap<CreateCommentRequest, Comment>();
        CreateMap<UpdateCommentRequest, Comment>().ReverseMap();
        CreateMap<Comment, CommentResponse>();

        CreateMap<Reply, ReplyResponse>();
        CreateMap<CreateReplyRequest, Reply>();
        CreateMap<UpdateReplyRequest, Reply>();
    }
}
