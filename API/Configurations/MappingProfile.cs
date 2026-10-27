using Application.Dtos.AppointmentDtos;
using Application.Dtos.AuthDtos;
using Application.Dtos.CommentsDtos;
using Application.Dtos.DepressionTestDtos;
using Application.Dtos.NotificationDtos;
using Application.Dtos.PostsDto;
using Application.Dtos.ReplyDtos;
using Application.Dtos.UserDtos;
using AutoMapper;
using Domain.Entities;
using Shared;

namespace API.Configurations;

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

		CreateMap<User, UserInfoResponse>();
		CreateMap<Admin, UserInfoResponse>();
		CreateMap<Doctor, DoctorInfoResponse>();

		CreateMap<Notification, NotificationResponse>()
			.ForMember(dest => dest.Resources, src => src.MapFrom(m => m.ResourcesObject));

		CreateMap<RegisterationRequest, BaseUser>()
			.IncludeAllDerived();

		CreateMap<RegisterationRequest, User>();
		CreateMap<RegisterationRequest, Doctor>();
		CreateMap<RegisterationRequest, Admin>();

		CreateMap<Appointment, AppointmentResponse>()
			// SQL Server returns DateTime values without a kind. Appointment
			// instants are UTC by contract, so keep the trailing "Z" in JSON;
			// otherwise clients interpret the value in their own time zone.
			.ForMember(dest => dest.StartTime, src =>
				src.MapFrom(appointment => DateTime.SpecifyKind(appointment.StartTime, DateTimeKind.Utc)));
		CreateMap<CreateAppointmentRequest, Appointment>()
			.ForMember(dest => dest.Duration, src => src.Ignore())
			.ForMember(dest => dest.Fees, src => src.Ignore());

		CreateMap<DepressionTestResult, DepressionTestResultResponse>();
	}
}
