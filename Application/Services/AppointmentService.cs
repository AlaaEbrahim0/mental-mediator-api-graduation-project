using Application.Contracts;
using Application.Dtos.AppointmentDtos;
using Application.Dtos.NotificationDtos;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using Shared;
using Shared.RequestParameters;

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
	private readonly IDoctorSchedulingService _schedulingService;
	private readonly ILogger<AppointmentService> _logger;

	public AppointmentService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService, IHateSpeechDetector hateSpeechDetector, IStorageService storageService, ICacheService cacheService, INotificationService notificationService, IMailService mailService, IDoctorSchedulingService schedulingService, ILogger<AppointmentService> logger)
	{
		_repos = repos;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
		_hateSpeechDetector = hateSpeechDetector;
		_storageService = storageService;
		_cacheService = cacheService;
		_notificationService = notificationService;
		_mailService = mailService;
		_schedulingService = schedulingService;
		_logger = logger;
	}

	/// <summary>
	/// Sends the email that accompanies an appointment state change. The
	/// appointment itself is already persisted and the in-app notification is
	/// already delivered at this point, so a mail failure is logged and
	/// swallowed: reporting it as a failed request would tell the client the
	/// operation did not happen, and a retry would duplicate the appointment.
	/// </summary>
	private async Task SendAppointmentEmailAsync(MailRequest mailRequest)
	{
		var mailResult = await _mailService.SendEmailAsync(mailRequest);

		if (mailResult.IsFailure)
		{
			_logger.LogWarning(
				"Appointment email \"{Subject}\" to {Recipient} was not delivered: {Reason}",
				mailRequest.Subject,
				mailRequest.ToEmail,
				mailResult.Error.Description);
		}
	}

	public async Task<Result<AppointmentResponse?>> CancelAppointment(int id, string? cancellationReason)
	{
		var appointment = await _repos.Appointments.GetById(id, true);
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

		_repos.Appointments.UpdateAppointment(appointment);

		var notification = Notification.CreateNotification(
			appointment.DoctorId,
			"Appointment Cancelled",
			$"{_userClaimsService.GetUserName()} cancelled the appointment",
			new Dictionary<string, int> { { "appointmentId", appointment.Id } },
			_userClaimsService.GetUserName(),
			_userClaimsService.GetPhotoUrl(),
			NotificationType.AppointmentCancellation,
			relatedResourceId: appointment.Id
			);

		await _notificationService.SendNotificationAsync(notification);

		var mailRequest = new MailRequest
		{
			ToEmail = appointment.DoctorEmail,
			Subject = "Appointment Cancellation",
			Body = $"The appointment with ID {appointment.Id} has been cancelled by {appointment.ClientEmail} [{appointment.ClientName}]."
		};
		await SendAppointmentEmailAsync(mailRequest);

		var appointmentResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentResponse;
	}

	public async Task<Result<AppointmentResponse?>> ConfirmAppointment(int id)
	{
		var appointment = await _repos.Appointments.GetById(id, true);
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

		_repos.Appointments.UpdateAppointment(appointment);

		var notification = Notification.CreateNotification(
			appointment.UserId,
			"Appointment Confirmed",
			$"{_userClaimsService.GetUserName()} confirmed the appointment",
			new Dictionary<string, int> { { "appointmentId", appointment.Id } },
			_userClaimsService.GetUserName(),
			_userClaimsService.GetPhotoUrl(),
			NotificationType.AppointmentConfirmation,
			relatedResourceId: appointment.Id
			);

		await _notificationService.SendNotificationAsync(notification);

		var mailRequest = new MailRequest
		{
			ToEmail = appointment.ClientEmail,
			Subject = "Appointment Confirmation",
			Body = $"The appointment with ID {appointment.Id} has been confirmed by Dr. {appointment.DoctorName}."
		};
		await SendAppointmentEmailAsync(mailRequest);

		var appointmentResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentResponse;
	}

	public async Task<Result<AppointmentResponse?>> CompleteAppointment(int id)
	{
		var appointment = await _repos.Appointments.GetById(id, true);
		if (appointment is null) return Error.NotFound("Appointments.NotFound", $"appointment: {id} doesn't exist");
		if (appointment.DoctorId != _userClaimsService.GetUserId()) return Error.Forbidden("Appointments.Forbidden", "Only the assigned doctor can complete this appointment.");
		var completionResult = appointment.Complete();
		if (completionResult.IsFailure) return completionResult.Error;
		_repos.Appointments.UpdateAppointment(appointment);
		await _repos.SaveAsync();
		return _mapper.Map<AppointmentResponse>(appointment);
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
		var startUtc = AsUtc(request.StartTime);

		Appointment appointment;

		// Revalidate availability while holding the per-doctor lock, then insert
		// and commit before releasing it. Two concurrent requests for the same
		// slot therefore cannot both pass validation and book.
		await using (var bookingLock = await _repos.Appointments.AcquireBookingLockAsync(doctorId))
		{
			var bookingValidation = await _schedulingService.ValidateBookingTimeAsync(
				doctorId,
				startUtc,
				request.Duration);

			if (bookingValidation.IsFailure)
			{
				return bookingValidation.Error;
			}

			appointment = _mapper.Map<Appointment>(request);
			appointment.StartTime = startUtc;
			appointment.Duration = bookingValidation.Value;
			appointment.Fees = doctor.SessionFees;
			appointment.DoctorId = doctorId;
			appointment.UserId = userId;
			appointment.Status = AppointmentStatus.Pending;
			appointment.DoctorName = doctor.FullName;
			appointment.DoctorEmail = doctor.Email!;
			appointment.ClientEmail = userEmail;
			appointment.ClientName = userName;
			appointment.DoctorPhotoUrl = doctor.PhotoUrl;
			appointment.ClientPhotoUrl = photoUrl;

			_repos.Appointments.CreateAppointment(appointment);
			await _repos.SaveAsync();
			await bookingLock.CommitAsync();
		}

		var notification = Notification.CreateNotification(
			doctorId,
			"New Appointment Request",
			$"{userName} requested an appointment with you",
			new Dictionary<string, int> { { "appointmentId", appointment.Id } },
			userName,
			photoUrl,
			NotificationType.AppointmentRequest,
			relatedResourceId: appointment.Id
			);

		await _notificationService.SendNotificationAsync(notification);

		var mailRequest = new MailRequest
		{
			ToEmail = doctor.Email,
			Subject = "New Appointment Request",
			Body = $"You have a new appointment request from {userName}. Appointment ID: {appointment.Id}."
		};
		await SendAppointmentEmailAsync(mailRequest);

		var appointmentResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentResponse;
	}

	private static DateTime AsUtc(DateTime value)
	{
		return value.Kind switch
		{
			DateTimeKind.Utc => value,
			DateTimeKind.Local => value.ToUniversalTime(),
			_ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
		};
	}

	public async Task<Result<AppointmentResponse?>> RejectAppointment(int id, RejectAppointmentRequest request)
	{
		var appointment = await _repos.Appointments.GetById(id, true);
		if (appointment is null)
		{
			return Error.NotFound("Appointments.NotFound", $"appointment: {id} doesn't exist");
		}

		var userId = _userClaimsService.GetUserId();
		if (appointment.DoctorId != userId)
		{
			return Error.Forbidden("Appointments.Forbidden", "you can't reject an appointment that you aren't part in");
		}

		var rejectionResult = appointment.Reject(request.RejectionReason);
		if (rejectionResult.IsFailure)
		{
			return rejectionResult.Error;
		}

		_repos.Appointments.UpdateAppointment(appointment);

		var notification = Notification.CreateNotification(
			appointment.UserId,
			"Appointment Rejected",
			$"{_userClaimsService.GetUserName()} rejected the appointment",
			new Dictionary<string, int> { { "appointmentId", appointment.Id } },
			_userClaimsService.GetUserName(),
			_userClaimsService.GetPhotoUrl(),
			NotificationType.AppointmentRejection,
			relatedResourceId: appointment.Id
			);

		await _notificationService.SendNotificationAsync(notification);

		var mailRequest = new MailRequest
		{
			ToEmail = appointment.ClientEmail,
			Subject = "Appointment Rejection",
			Body = $"Your appointment with ID {appointment.Id} has been rejected by Dr. {appointment.DoctorName}. Reason: {request.RejectionReason}."
		};
		await SendAppointmentEmailAsync(mailRequest);

		var appointmentResponse = _mapper.Map<AppointmentResponse>(appointment);
		return appointmentResponse;
	}

	public async Task<Result<List<AppointmentResponse>>> GetAppointments(AppointmentRequestParameters request)
	{
		var appointments = await _repos.Appointments.GetAll(request, false);
		var appointmentsResponse = _mapper.Map<List<AppointmentResponse>>(appointments);
		return appointmentsResponse;
	}

	public async Task<Result<AppointmentResponse?>> GetAppointment(int id)
	{
		var appointment = await _repos.Appointments.GetById(id, false);
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

	public async Task<Result<List<AppointmentResponse>>> GetClientAppointments(MyAppointmentsRequestParameters request)
	{
		var clientId = _userClaimsService.GetUserId();
		var appointments = await _repos.Appointments.GetByUserId(clientId, request, false);
		var appointmentsResponse = _mapper.Map<List<AppointmentResponse>>(appointments);
		return appointmentsResponse;
	}

	public async Task<Result<List<AppointmentResponse>>> GetDoctorAppointments(MyAppointmentsRequestParameters request)
	{
		var doctorId = _userClaimsService.GetUserId();
		var appointments = await _repos.Appointments.GetByDoctorId(doctorId, request, false);
		var appointmentsResponse = _mapper.Map<List<AppointmentResponse>>(appointments);
		return appointmentsResponse;
	}


}
