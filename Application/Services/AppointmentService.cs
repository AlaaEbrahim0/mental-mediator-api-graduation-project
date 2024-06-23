using Application.Contracts;
using Application.Dtos.AppointmentDtos;
using Application.Dtos.NotificationDtos;
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
	private readonly IMailService _mailService;

	public AppointmentService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService, IHateSpeechDetector hateSpeechDetector, IStorageService storageService, ICacheService cacheService, INotificationService notificationService, IMailService mailService)
	{
		_repos = repos;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
		_hateSpeechDetector = hateSpeechDetector;
		_storageService = storageService;
		_cacheService = cacheService;
		_notificationService = notificationService;
		_mailService = mailService;
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

		var cancellationResult = appointment.Cancel(cancellationReason);
		if (cancellationResult.IsFailure)
		{
			return cancellationResult.Error;
		}

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

		var mailRequest = new MailRequest
		{
			ToEmail = appointment.DoctorEmail,
			Subject = "Appointment Cancellation",
			Body = $"The appointment with ID {appointment.Id} has been cancelled by {appointment.ClientEmail} [{appointment.ClientName}]."
		};
		await _mailService.SendEmailAsync(mailRequest);

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
			return Error.Forbidden("Appointments.Forbidden", "you can't confirm an appointment that you aren't part in");
		}

		var confirmationResult = appointment.Confirm();
		if (confirmationResult.IsFailure)
		{
			return confirmationResult.Error;
		}

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

		var mailRequest = new MailRequest
		{
			ToEmail = appointment.ClientEmail,
			Subject = "Appointment Confirmation",
			Body = $"The appointment with ID {appointment.Id} has been confirmed by Dr. {appointment.DoctorName}."
		};
		await _mailService.SendEmailAsync(mailRequest);

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
		var userEmail = _userClaimsService.GetEmail();
		var appointment = _mapper.Map<Appointment>(request);

		appointment.DoctorId = doctorId;
		appointment.UserId = userId;
		appointment.Status = AppointmentStatus.Pending;
		appointment.DoctorName = doctor.FullName;
		appointment.DoctorEmail = doctor.Email!;
		appointment.ClientEmail = userEmail;
		appointment.ClientName = userName;
		appointment.DoctorPhotoUrl = doctor.PhotoUrl;
		appointment.ClientPhotoUrl = photoUrl;

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

		var mailRequest = new MailRequest
		{
			ToEmail = doctor.Email,
			Subject = "New Appointment Request",
			Body = $"You have a new appointment request from {userName}. Appointment ID: {appointment.Id}."
		};
		await _mailService.SendEmailAsync(mailRequest);

		var appointmentResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentResponse;
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
			return Error.Forbidden("Appointments.Forbidden", "you can't reject an appointment that you aren't part in");
		}

		var rejectionResult = appointment.Reject(rejectionReason);
		if (rejectionResult.IsFailure)
		{
			return rejectionResult.Error;
		}

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

		var mailRequest = new MailRequest
		{
			ToEmail = appointment.ClientEmail,
			Subject = "Appointment Rejection",
			Body = $"Your appointment with ID {appointment.Id} has been rejected by Dr. {appointment.DoctorName}. Reason: {rejectionReason}."
		};
		await _mailService.SendEmailAsync(mailRequest);

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
		var userRole = _userClaimsService.GetRole();
		if (userRole != "Admin" && appointment.UserId != userId && appointment.DoctorId != userId)
		{
			return Error.Forbidden("Appointments.Forbidden", "you can't access this resource");
		}

		var appointmentsResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentsResponse;
	}

	public async Task<Result<List<AppointmentResponse>>> GetClientAppointments(RequestParameters request)
	{
		var clientId = _userClaimsService.GetUserId();
		var appointments = await _repos.Appointements.GetByUserId(clientId, request, false);
		var appointmentsResponse = _mapper.Map<List<AppointmentResponse>>(appointments);
		return appointmentsResponse;
	}

	public async Task<Result<List<AppointmentResponse>>> GetDoctorAppointments(RequestParameters request)
	{
		var doctorId = _userClaimsService.GetUserId();
		var appointments = await _repos.Appointements.GetByDoctorId(doctorId, request, false);
		var appointmentsResponse = _mapper.Map<List<AppointmentResponse>>(appointments);
		return appointmentsResponse;
	}
}
