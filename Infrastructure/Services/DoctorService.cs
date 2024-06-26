using Application.Contracts;
using Application.Dtos.UserDtos;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Shared;

namespace Infrastructure.Services;

public class DoctorService : IDoctorService
{
	private readonly IRepositoryManager _repoManager;
	private readonly IUserClaimsService
		_userClaimsService;
	private readonly IStorageService _storageService;
	private readonly IMapper _mapper;
	private readonly IWeeklyScheduleService _weeklyScheduleService;

	public DoctorService(IUserClaimsService userClaimsService, IStorageService storageService, IMapper mapper, IRepositoryManager repoManager, IWeeklyScheduleService weeklyScheduleService)
	{
		_userClaimsService = userClaimsService;
		_storageService = storageService;
		_mapper = mapper;
		_repoManager = repoManager;
		_weeklyScheduleService = weeklyScheduleService;
	}

	public IWeeklyScheduleService WeeklyScheduleService => _weeklyScheduleService;

	public async Task<Result<DoctorInfoResponse>> GetDoctorInfo(string id)
	{
		var user = await _repoManager.Doctors.GetById(id, false);

		if (user is null)
		{
			return UserErrors.NotFound(id);
		}

		var response = _mapper.Map<DoctorInfoResponse>(user);
		return response;

	}
	public async Task<Result<DoctorInfoResponse>> GetCurrentDoctorInfo()
	{
		var currentUserId = _userClaimsService.GetUserId();
		var user = await _repoManager.Doctors.GetById(currentUserId, false);

		if (user is null)
		{
			return UserErrors.NotFound(currentUserId);
		}

		var response = _mapper.Map<DoctorInfoResponse>(user);
		return response;

	}

	public async Task<Result<DoctorInfoResponse>> UpdateDoctorInfo(string doctorId, UpdateDoctorInfoRequest request)
	{
		var user = await _repoManager.Doctors.GetById(doctorId, true);

		if (user is null)
		{
			return UserErrors.NotFound(doctorId);
		}

		if (request.Photo is not null)
		{
			var uploadResult = await _storageService.UploadPhoto(request.Photo);
			if (uploadResult.IsFailure)
			{
				return uploadResult.Error;
			}
			user.PhotoUrl = uploadResult.Value;
		}

		_mapper.Map(request, user);
		_repoManager.Doctors.UpdateDoctor(user);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorInfoResponse>(user);
		return response;
	}
	public async Task<Result<DoctorInfoResponse>> UpdateCurrentDoctorInfo(UpdateDoctorInfoRequest request)
	{
		var currentUserId = _userClaimsService.GetUserId();
		var user = await _repoManager.Doctors.GetById(currentUserId, true);

		if (user is null)
		{
			return UserErrors.NotFound(currentUserId);
		}

		if (request.Photo is not null)
		{
			var uploadResult = await _storageService.UploadPhoto(request.Photo);
			if (uploadResult.IsFailure)
			{
				return uploadResult.Error;
			}
			user.PhotoUrl = uploadResult.Value;
		}

		_mapper.Map(request, user);
		_repoManager.Doctors.UpdateDoctor(user);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorInfoResponse>(user);
		return response;
	}

	public async Task<Result<IEnumerable<DoctorInfoResponse>>> GetAll(DoctorRequestParameters requestParameters)
	{
		var doctors = await _repoManager.Doctors.GetAll(requestParameters, false);
		var response = _mapper.Map<List<DoctorInfoResponse>>(doctors);
		return response;
	}

	public async Task<Result<DoctorInfoResponse>> DeleteDoctor(string doctorId)
	{
		var doctor = await _repoManager.Doctors.GetById(doctorId, true);

		if (doctor is null)
		{
			return UserErrors.NotFound(doctorId);
		}

		doctor.isDeleted = true;
		_repoManager.Doctors.UpdateDoctor(doctor);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorInfoResponse>(doctor);
		return response;
	}

	public async Task<Result<List<TimeSpan>>> GetAvailableSlots(string doctorId, DateTime date)
	{
		var appointments = await _repoManager.Appointements.GetByDoctorIdAndDate(doctorId, date, false);

		var doctorScheduleWeekday = await _repoManager.DoctorSchedule.GetScheduleWeekDay(doctorId, date.DayOfWeek, false);

		if (doctorScheduleWeekday is null)
		{
			return Enumerable.Empty<TimeSpan>().ToList();
		}

		var slots = DivideTimeSpanInterval(doctorScheduleWeekday.StartTime, doctorScheduleWeekday.EndTime, doctorScheduleWeekday.SessionDuration);

		var availableSlots = FilterAvailableSlots(slots, appointments.ToList());

		return availableSlots;
	}

	private List<TimeSpan> DivideTimeSpanInterval(TimeSpan startTime, TimeSpan endTime, TimeSpan intervalDuration)
	{
		List<TimeSpan> intervals = new List<TimeSpan>();

		TimeSpan currentTime = startTime;

		while (currentTime < endTime)
		{
			intervals.Add(currentTime);
			currentTime = currentTime.Add(intervalDuration);
		}

		if (currentTime <= endTime)
		{
			intervals.Add(endTime);
		}

		return intervals;
	}

	private List<TimeSpan> FilterAvailableSlots(List<TimeSpan> slots, List<Appointment> appointments)
	{
		List<TimeSpan> availableSlots = new List<TimeSpan>();

		foreach (var slot in slots)
		{
			bool isSlotAvailable = true;

			foreach (var appointment in appointments)
			{
				if (IsOverlap(slot, slot.Add(appointment.Duration), appointment.StartTime, appointment.EndTime))
				{
					isSlotAvailable = false;
					break;
				}
			}

			if (isSlotAvailable)
			{
				availableSlots.Add(slot);
			}
		}

		return availableSlots;
	}

	private bool IsOverlap(TimeSpan slotStart, TimeSpan slotEnd, DateTime appointmentStart, DateTime appointmentEnd)
	{
		// Convert slotStart and slotEnd to DateTime for comparison
		DateTime slotStartTime = DateTime.Today.Add(slotStart);
		DateTime slotEndTime = DateTime.Today.Add(slotEnd);

		// Check if there is any overlap between two time intervals
		return slotStartTime < appointmentEnd && slotEndTime > appointmentStart;
	}
}
