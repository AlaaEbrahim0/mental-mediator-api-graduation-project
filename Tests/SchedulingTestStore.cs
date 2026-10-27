using Application.Contracts;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace MentalMediator.Tests;

/// <summary>
/// In-memory fixture for the scheduling tests. The repositories under test are
/// the production ones, backed by EF Core's in-memory provider, so the query
/// shapes (time-off overlap, active-appointment filtering, period ordering)
/// are exercised rather than mocked.
/// </summary>
public sealed class SchedulingTestStore : IDisposable
{
	/// <summary>The next Monday at least a week away, so slot generation is never filtered as past.</summary>
	public static DateOnly NextMonday()
	{
		var today = DateTime.UtcNow.Date;
		var offset = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
		if (offset == 0)
		{
			offset = 7;
		}

		return DateOnly.FromDateTime(today.AddDays(offset));
	}

	public static DateTime Utc(DateOnly date, int hour, int minute = 0)
	{
		return DateTime.SpecifyKind(date.ToDateTime(new TimeOnly(hour, minute)), DateTimeKind.Utc);
	}

	public AppDbContext Context { get; }

	public Doctor Doctor { get; }

	public DoctorSchedulingService Service { get; }

	public SchedulingTestStore(string timeZoneId = "UTC", int consultationDurationMinutes = 30)
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"scheduling-{Guid.NewGuid()}")
			.Options;

		Context = new AppDbContext(options);

		Doctor = new Doctor
		{
			Id = "doctor-1",
			Email = "doctor@example.com",
			UserName = "doctor",
			NormalizedEmail = "DOCTOR@EXAMPLE.COM",
			NormalizedUserName = "DOCTOR",
			FirstName = "Dana",
			LastName = "Doctor",
			Gender = "female",
			IsAvailable = true,
			SessionFees = 100m,
			Specialization = DoctorSpecialization.ClinicalPsychology,
			PasswordHash = "AQAAAAEAACcQAAAAENpN7x1w8gfN0zbLDn1VbWcQv6RzjPm+bK0v4bNwqzBn8ZLhF6cD4sPc2UQdAy5aSg==",
			SecurityStamp = Guid.NewGuid().ToString("N"),
			ConcurrencyStamp = Guid.NewGuid().ToString("N"),
		};

		Context.Doctors.Add(Doctor);
		Context.DoctorSchedulingSettings.Add(new DoctorSchedulingSettings
		{
			DoctorId = Doctor.Id,
			TimeZoneId = timeZoneId,
			ConsultationDurationMinutes = consultationDurationMinutes,
		});
		Context.SaveChanges();

		Service = new DoctorSchedulingService(
			new RepositoryManager(Context),
			new TestUserClaimsService(Doctor.Id));
	}

	public DoctorSchedulingService ServiceFor(string userId)
	{
		return new DoctorSchedulingService(
			new RepositoryManager(Context),
			new TestUserClaimsService(userId));
	}

	public void AddPeriod(DayOfWeek day, TimeSpan start, TimeSpan end)
	{
		Context.DoctorAvailabilityPeriods.Add(new DoctorAvailabilityPeriod
		{
			DoctorId = Doctor.Id,
			DayOfWeek = day,
			StartTime = start,
			EndTime = end,
		});
		Context.SaveChanges();
	}

	public void AddTimeOff(DateTime startUtc, DateTime endUtc, string? reason = null)
	{
		Context.DoctorTimeOff.Add(new DoctorTimeOff
		{
			DoctorId = Doctor.Id,
			StartTimeUtc = startUtc,
			EndTimeUtc = endUtc,
			Reason = reason,
			CreatedAtUtc = DateTime.UtcNow,
		});
		Context.SaveChanges();
	}

	public void AddAppointment(
		DateTime startUtc,
		TimeSpan duration,
		AppointmentStatus status = AppointmentStatus.Confirmed)
	{
		Context.Appointments.Add(new Appointment
		{
			DoctorId = Doctor.Id,
			UserId = "patient-1",
			StartTime = startUtc,
			Duration = duration,
			Status = status,
			Location = "Online",
		});
		Context.SaveChanges();
	}

	public void Dispose() => Context.Dispose();
}

public sealed class TestUserClaimsService : IUserClaimsService
{
	private readonly string _userId;

	public TestUserClaimsService(string userId) => _userId = userId;

	public string GetUserId() => _userId;
	public string GetUserName() => "doctor";
	public string GetRole() => "Doctor";
	public string GetPhotoUrl() => string.Empty;
	public string GetEmail() => "doctor@example.com";
}
