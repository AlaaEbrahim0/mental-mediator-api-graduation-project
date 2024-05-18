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
		CreateMap<UpdateDoctorInfoRequest, Doctor>();
		CreateMap<Doctor, DoctorInfoResponse>();

		CreateMap<Notification, NotificationResponse>()
		.ForMember(dest => dest.Resources, src =>
				src.MapFrom(m => m.ResourcesObject));

		CreateMap<RegisterationRequest, BaseUser>()
			.IncludeAllDerived();

		CreateMap<RegisterationRequest, User>();
		CreateMap<RegisterationRequest, Doctor>();

		CreateMap<CreateScheduleWeekDayRequest, DoctorScheduleWeekDay>();
		CreateMap<CreateDoctorWeeklyScheduleRequest, List<DoctorScheduleWeekDay>>()
			.ForMember(x => x, src => src.MapFrom(x => x.WeekDays));

		CreateMap<List<DoctorScheduleWeekDay>, DoctorWeeklyScheduleResponse>()
			.ForMember(x => x.WeekDays, src => src.MapFrom(x => x))
			.ForMember(x => x.DoctorId, src => src.MapFrom(x => x.Select(x => x.DoctorId).First()));


	}
}
