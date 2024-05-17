namespace API.Configurations;

using Application.Dtos.AuthDtos;
using Application.Dtos.CommentsDtos;
using Application.Dtos.PostsDto;
using Application.Dtos.ReplyDtos;
using Application.Dtos.UserDtos;
using Application.Dtos.WeeklyScheduleDtos;
using AutoMapper;
using Domain.Entities;
using Shared;


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
		CreateMap<UpdateDoctorInfoRequest, Doctor>()
			.ForMember(dest => dest.WeeklySchedule, src => src.Ignore());

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

		CreateMap<AvailableDays, AvailableDayResponse>();
		CreateMap<WeeklySchedule, DoctorWeeklyScheduleResponse>();

		CreateMap<CreateAvailableDayRequest, AvailableDays>();
		CreateMap<UpdateAvailableDayRequest, AvailableDays>();

		CreateMap<CreateDoctorWeeklyScheduleRequest, WeeklySchedule>();

	}
}
