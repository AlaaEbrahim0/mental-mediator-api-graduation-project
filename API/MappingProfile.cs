namespace API;
using AutoMapper;
using Domain.Entities;
using Shared;
using Shared.AuthDtos;
using Shared.CommentsDtos;
using Shared.PostsDto;
using Shared.ReplyDtos;
using Shared.UserDtos;

public class MappingProfile : Profile
{

	public MappingProfile()
	{

		CreateMap<Post, PostResponse>();
		CreateMap<CreatePostRequest, Post>();
		CreateMap<UpdatePostRequest, Post>().ReverseMap();

		CreateMap<CreateCommentRequest, Comment>();
		CreateMap<UpdateCommentRequest, Comment>().ReverseMap();
		CreateMap<Comment, CommentResponse>();

		CreateMap<Reply, ReplyResponse>();
		CreateMap<CreateReplyRequest, Reply>();
		CreateMap<UpdateReplyRequest, Reply>();

		CreateMap<UpdateUserInfoRequest, AppUser>();
		CreateMap<AppUser, UserInfoResponse>();

		CreateMap<RegistrationRequest, AppUser>();

		CreateMap<Notification, NotificationResponse>();
	}
}
