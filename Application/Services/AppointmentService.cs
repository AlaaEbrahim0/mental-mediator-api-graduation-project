using Application.Contracts;
using Application.Dtos.AppointmentDtos;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Shared;

namespace Application.Services;

public class AppointmentService : IAppointmentService
{
	private readonly IRepositoryManager _repos;
	private readonly IMapper _mapper;
	private readonly IUserClaimsService _userClaimsService;
	private readonly IHateSpeechDetector _hateSpeechDetector;
	private readonly IStorageService _storageService;
	private readonly ICacheService _cacheService;
	private readonly INotificationService _notificationService;

	public AppointmentService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService, IHateSpeechDetector hateSpeechDetector, IStorageService storageService, ICacheService cacheService, INotificationService notificationService)
	{
		_repos = repos;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
		_hateSpeechDetector = hateSpeechDetector;
		_storageService = storageService;
		_cacheService = cacheService;
		_notificationService = notificationService;
	}

	public async Task<Result<AppointmentResponse?>> CancelAppointment(int id, string? cancellationReason)
	{
		var appointment = await _repos.Appointements.GetById(id, true);
		if (appointment is null)
		{
			return Error.NotFound("Appointments.NotFound", $"appointment: {id} doesn't exist");
		}
		var userId = _userClaimsService.GetUserId();
		if (appointment.UserId != userId)
		{
			return Error.Forbidden("Appointments.Forbidden", "you can't cancel an appointment that you aren't part in");
		}
		appointment.Status = AppointmentStatus.Cancelled;
		appointment.CancellationReason = cancellationReason;
		_repos.Appointements.UpdateAppointment(appointment);

		var notification = Notification.CreateNotification(
			appointment.DoctorId,
			$"{_userClaimsService.GetUserName()} cancelled the appointment",
			new Dictionary<string, int> { { "appointmentId", appointment.Id } },
			_userClaimsService.GetUserName(),
			_userClaimsService.GetPhotoUrl(),
			NotificationType.AppointmentCancellation
			);
		_repos.Notifications.CreateNotification(notification);
		await _repos.SaveAsync();

		await _notificationService.SendNotificationAsync(notification);

		var appointmentResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentResponse;

	}

	public async Task<Result<AppointmentResponse?>> ConfirmAppointment(int id)
	{
		var appointment = await _repos.Appointements.GetById(id, true);
		if (appointment is null)
		{
			return Error.NotFound("Appointments.NotFound", $"appointment: {id} doesn't exist");
		}
		var userId = _userClaimsService.GetUserId();
		if (appointment.DoctorId != userId)
		{
			return Error.Forbidden("Appointments.Forbidden", "you can't cancel an appointment that you aren't part in");
		}
		appointment.Status = AppointmentStatus.Confirmed;
		_repos.Appointements.UpdateAppointment(appointment);

		var notification = Notification.CreateNotification(
			appointment.UserId,
			$"{_userClaimsService.GetUserName()} confirmed the appointment",
			new Dictionary<string, int> { { "appointmentId", appointment.Id } },
			_userClaimsService.GetUserName(),
			_userClaimsService.GetPhotoUrl(),
			NotificationType.AppointmentConfirmation
			);
		_repos.Notifications.CreateNotification(notification);
		await _repos.SaveAsync();
		await _notificationService.SendNotificationAsync(notification);

		var appointmentResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentResponse;
	}

	public async Task<Result<AppointmentResponse>> CreateAppointment(string doctorId, CreateAppointmentRequest request)
	{
		var doctor = await _repos.Doctors.GetById(doctorId, true);
		if (doctor is null)
		{
			return Error.NotFound("Doctors.NotFound", $"doctor with id [{doctorId}] isn't found");
		}
		var userId = _userClaimsService.GetUserId();
		var userName = _userClaimsService.GetUserName();
		var photoUrl = _userClaimsService.GetPhotoUrl();
		var appointment = _mapper.Map<Appointment>(request);
		appointment.DoctorId = doctorId;
		appointment.UserId = userId;
		appointment.Status = AppointmentStatus.Pending;
		appointment.DoctorName = doctor.FullName;
		appointment.ClientName = userName;
		appointment.DoctorPhotoUrl = doctor.PhotoUrl;

		_repos.Appointements.CreateAppointment(appointment);
		await _repos.SaveAsync();

		var notification = Notification.CreateNotification(
			doctorId,
			$"{userName} requested an appointment with you",
			new Dictionary<string, int> { { "appointmentId", appointment.Id } },
			userName,
			photoUrl,
			NotificationType.AppointmentRequest
			);
		_repos.Notifications.CreateNotification(notification);
		await _repos.SaveAsync();


		await _notificationService.SendNotificationAsync(notification);

		var appointmentResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentResponse;
	}

	public async Task<Result<List<AppointmentResponse>>> GetAppointements(RequestParameters request)
	{
		var appointments = await _repos.Appointements.GetAll(request, false);
		var appointmentsResponse = _mapper.Map<List<AppointmentResponse>>(appointments);
		return appointmentsResponse;
	}

	public async Task<Result<AppointmentResponse?>> GetAppointment(int id)
	{
		var appointment = await _repos.Appointements.GetById(id, false);
		if (appointment is null)
		{
			return Error.NotFound("Appointments.NotFound", $"appointment: {id} doesn't exist");
		}

		var userId = _userClaimsService.GetUserId();
		if (appointment.UserId != userId && appointment.DoctorId != userId)
		{
			return Error.Forbidden("Appointments.Forbidden", "you can't access this resource");
		}
		var appointmentsResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentsResponse;
	}

	public async Task<Result<List<AppointmentResponse>>> GetClientAppointments(string clientId, RequestParameters request)
	{
		var appointments = await _repos.Appointements.GetByUserId(clientId, request, false);
		var userId = _userClaimsService.GetUserId();
		if (clientId != userId)
		{
			return Error.Forbidden("Appointments.Forbidden", "you can't access this resource");
		}
		var appointmentsResponse = _mapper.Map<List<AppointmentResponse>>(appointments);
		return appointmentsResponse;
	}

	public async Task<Result<List<AppointmentResponse>>> GetDoctorAppointments(string doctorId, RequestParameters request)
	{
		var appointments = await _repos.Appointements.GetByDoctorId(doctorId, request, false);
		var userId = _userClaimsService.GetUserId();
		if (doctorId != userId)
		{
			return Error.Forbidden("Appointments.Forbidden", "you can't access this resource");
		}
		var appointmentsResponse = _mapper.Map<List<AppointmentResponse>>(appointments);
		return appointmentsResponse;
	}

	public async Task<Result<AppointmentResponse?>> RejectAppointment(int id, string? rejectionReason)
	{
		var appointment = await _repos.Appointements.GetById(id, true);
		if (appointment is null)
		{
			return Error.NotFound("Appointments.NotFound", $"appointment: {id} doesn't exist");
		}

		var userId = _userClaimsService.GetUserId();
		if (appointment.DoctorId != userId)
		{
			return Error.Forbidden("Appointments.Forbidden", "you can't cancel an appointment that you aren't part in");
		}
		appointment.Status = AppointmentStatus.Rejected;
		appointment.RejectionReason = rejectionReason;
		_repos.Appointements.UpdateAppointment(appointment);

		var notification = Notification.CreateNotification(
			appointment.UserId,
			$"{_userClaimsService.GetUserName()} rejected the appointment",
			new Dictionary<string, int> { { "appointmentId", appointment.Id } },
			_userClaimsService.GetUserName(),
			_userClaimsService.GetPhotoUrl(),
			NotificationType.AppointmentRejection
			);
		_repos.Notifications.CreateNotification(notification);
		await _repos.SaveAsync();
		await _notificationService.SendNotificationAsync(notification);

		var appointmentResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentResponse;
	}


}

