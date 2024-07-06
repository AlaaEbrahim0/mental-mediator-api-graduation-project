using Application.Contracts;
using Application.Dtos;
using Application.Dtos.UserDtos;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Shared;

namespace Application.Services;

public class DoctorService : IDoctorService
{
	private readonly IRepositoryManager _repoManager;
	private readonly IUserClaimsService
		_userClaimsService;
	private readonly IStorageService _storageService;
	private readonly IMapper _mapper;

	public DoctorService(IUserClaimsService userClaimsService, IStorageService storageService, IMapper mapper, IRepositoryManager repoManager, IWeeklyScheduleService weeklyScheduleService)
	{
		_userClaimsService = userClaimsService;
		_storageService = storageService;
		_mapper = mapper;
		_repoManager = repoManager;
		WeeklyScheduleService = weeklyScheduleService;
	}

	public IWeeklyScheduleService WeeklyScheduleService { get; }

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

		doctor.IsDeleted = true;
		_repoManager.Doctors.UpdateDoctor(doctor);
		await _repoManager.SaveAsync();

		var response = _mapper.Map<DoctorInfoResponse>(doctor);
		return response;
	}

	public async Task<Result<List<TimeSpan>>> GetAvailableSlots(string doctorId, DateTime date)
	{
		var appointments = await _repoManager.Appointments.GetByDoctorIdAndDate(doctorId, date, false);
		var doctorScheduleWeekday = await _repoManager.DoctorSchedule.GetScheduleWeekDay(doctorId, date.DayOfWeek, false);

		if (doctorScheduleWeekday is null)
		{
			return Error.NotFound("NotFound", "No schedule available for the given date.");
		}

		var slots = DivideTimeSpanInterval(doctorScheduleWeekday.StartTime, doctorScheduleWeekday.EndTime, doctorScheduleWeekday.SessionDuration);
		var availableSlots = FilterAvailableSlots(slots, doctorScheduleWeekday.SessionDuration, appointments.ToList());

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

		return intervals;
	}

	private List<TimeSpan> FilterAvailableSlots(List<TimeSpan> slots, TimeSpan duration, List<Appointment> appointments)
	{
		List<TimeSpan> availableSlots = new List<TimeSpan>();

		foreach (var slot in slots)
		{
			bool isSlotAvailable = true;
			var slotEnd = slot.Add(duration);

			foreach (var appointment in appointments)
			{
				var appointmentStart = appointment.StartTime.TimeOfDay;
				var appointmentEnd = appointment.EndTime.TimeOfDay;

				if (IsOverlap(slot, slotEnd, appointmentStart, appointmentEnd))
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

	private bool IsOverlap(TimeSpan slotStart, TimeSpan slotEnd, TimeSpan appointmentStart, TimeSpan appointmentEnd)
	{
		return slotStart < appointmentEnd && slotEnd > appointmentStart;
	}

	public async Task<Result<DoctorReportResponse>> GetReports()
	{
		var doctorId = _userClaimsService.GetUserId();
		var stats = await _repoManager.Appointments.GetDoctorStats(doctorId);
		var (appointmentWeeklyCount, appointmentMonthlyCount) = await _repoManager.Appointments.GetDoctorAppointmentCounts(doctorId);
		var statusCount = await _repoManager.Appointments.GetAppointmentStatusCounts(doctorId);
		var depressionTestCount = await _repoManager.DepressionTestResults.CalculateResultCounts();
		var depressionTestAgeGroupDistribution = await _repoManager.DepressionTestResults.CalculateAgeGroupDistribution();
		var depressionTestGenderDistribution = await _repoManager.DepressionTestResults.CalculateGenderDistribution();

		var response = new DoctorReportResponse
		{
			TotalAppointments = stats.totalAppointments,
			TotalProfit = stats.totalProfit,
			AppointmentsPerMonth = appointmentMonthlyCount,
			AppointmentsPerWeekday = appointmentWeeklyCount,
			StatusCounts = statusCount,
			TestResultsAgeGroupDistributions = depressionTestAgeGroupDistribution,
			TestResultsCount = depressionTestCount,
			TestResultsGenderDistributions = depressionTestGenderDistribution
		};

		return response;
	}


}