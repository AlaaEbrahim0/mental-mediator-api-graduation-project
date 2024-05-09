namespace API;

using Application.Dtos.AuthDtos;
using Application.Dtos.UserDtos;
using AutoMapper;
using Domain.Entities;
using Shared;
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

		CreateMap<UpdateUserInfoRequest, User>();
		CreateMap<UpdateDoctorInfoRequest, Doctor>();

		CreateMap<BaseUser, DoctorInfoResponse>()
			.IncludeAllDerived();

		CreateMap<User, UserInfoResponse>();
		CreateMap<Doctor, DoctorInfoResponse>();

		CreateMap<Notification, NotificationResponse>()
		.ForMember(dest => dest.Resources, src =>
				src.MapFrom(m => m.ResourcesObject));

		CreateMap<RegisterationRequest, BaseUser>()
			.IncludeAllDerived();

		CreateMap<RegisterationRequest, User>();
		CreateMap<RegisterationRequest, Doctor>();

	}
}
