using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace MentalMediator.Tests;

public class BookingValidationTests
{
	private static readonly DateOnly Monday = SchedulingTestStore.NextMonday();

	private static TimeSpan Time(int hour, int minute = 0) => new(hour, minute, 0);

	private static SchedulingTestStore MondayStore(int durationMinutes = 30)
	{
		var store = new SchedulingTestStore(consultationDurationMinutes: durationMinutes);
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(17));
		return store;
	}

	[Fact]
	public async Task accepts_a_slot_on_the_configured_grid()
	{
		using var store = MondayStore();

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9),
			null);

		result.IsFailure.Should().BeFalse(result.Error.Description);
		result.Value.Should().Be(TimeSpan.FromMinutes(30));
	}

	[Fact]
	public async Task returns_the_configured_duration_when_the_client_omits_it()
	{
		using var store = MondayStore(45);

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9),
			null);

		result.IsFailure.Should().BeFalse(result.Error.Description);
		result.Value.Should().Be(TimeSpan.FromMinutes(45));
	}

	[Fact]
	public async Task rejects_a_client_supplied_duration_that_differs_from_the_doctor_setting()
	{
		using var store = MondayStore(30);

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9),
			TimeSpan.FromMinutes(60));

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.DurationMismatch");
	}

	[Fact]
	public async Task accepts_a_time_on_the_matching_duration_grid()
	{
		using var store = MondayStore(45);

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9, 45),
			TimeSpan.FromMinutes(45));

		result.IsFailure.Should().BeFalse(result.Error.Description);
	}

	[Fact]
	public async Task rejects_a_time_that_is_off_the_duration_grid()
	{
		using var store = MondayStore(45);

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9, 30),
			null);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.OutsideWorkingHours");
	}

	[Fact]
	public async Task rejects_a_time_outside_the_weekly_periods()
	{
		using var store = MondayStore();

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 18),
			null);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.OutsideWorkingHours");
	}

	[Fact]
	public async Task rejects_a_booking_that_would_cross_the_period_end()
	{
		using var store = MondayStore();

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 16, 45),
			null);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.OutsideWorkingHours");
	}

	[Fact]
	public async Task rejects_a_time_during_time_off()
	{
		using var store = MondayStore();
		store.AddTimeOff(
			SchedulingTestStore.Utc(Monday, 9),
			SchedulingTestStore.Utc(Monday, 10));

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9, 30),
			null);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.TimeOffConflict");
	}

	[Fact]
	public async Task rejects_a_time_overlapping_an_active_appointment()
	{
		using var store = MondayStore();
		store.AddAppointment(
			SchedulingTestStore.Utc(Monday, 9),
			TimeSpan.FromMinutes(30),
			AppointmentStatus.Pending);

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9),
			null);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.AppointmentConflict");
	}

	[Fact]
	public async Task accepts_the_slot_immediately_after_an_appointment()
	{
		using var store = MondayStore();
		store.AddAppointment(
			SchedulingTestStore.Utc(Monday, 9),
			TimeSpan.FromMinutes(30));

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9, 30),
			null);

		result.IsFailure.Should().BeFalse(result.Error.Description);
	}

	[Fact]
	public async Task ignores_cancelled_appointments()
	{
		using var store = MondayStore();
		store.AddAppointment(
			SchedulingTestStore.Utc(Monday, 9),
			TimeSpan.FromMinutes(30),
			AppointmentStatus.Cancelled);

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9),
			null);

		result.IsFailure.Should().BeFalse(result.Error.Description);
	}

	[Fact]
	public async Task rejects_a_booking_for_a_doctor_that_is_not_accepting_requests()
	{
		using var store = MondayStore();
		store.Doctor.IsAvailable = false;
		store.Context.SaveChanges();

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9),
			null);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.DoctorUnavailable");
	}

	[Fact]
	public async Task accepts_a_booking_again_once_the_doctor_is_available()
	{
		using var store = MondayStore();
		store.Doctor.IsAvailable = false;
		store.Context.SaveChanges();

		var unavailable = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9),
			null);
		unavailable.IsFailure.Should().BeTrue();

		store.Doctor.IsAvailable = true;
		store.Context.SaveChanges();

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 9),
			null);

		result.IsFailure.Should().BeFalse(result.Error?.Description ?? "accepted");
	}

	[Fact]
	public async Task rejects_a_missing_doctor_before_applying_the_availability_rule()
	{
		using var store = MondayStore();

		var result = await store.Service.ValidateBookingTimeAsync(
			"doctor-does-not-exist",
			SchedulingTestStore.Utc(Monday, 9),
			null);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Doctors.NotFound");
	}

	[Fact]
	public async Task validates_in_the_doctor_time_zone()
	{
		using var store = MondayStore();
		store.Context.DoctorSchedulingSettings.Single().TimeZoneId = "Asia/Riyadh";
		store.Context.SaveChanges();

		var result = await store.Service.ValidateBookingTimeAsync(
			store.Doctor.Id,
			SchedulingTestStore.Utc(Monday, 6),
			null);

		result.IsFailure.Should().BeFalse(result.Error.Description);
	}
}
