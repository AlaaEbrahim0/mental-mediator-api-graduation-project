using Application.Dtos.SchedulingDtos;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace MentalMediator.Tests;

public class ScheduleMutationTests
{
	private static readonly DateOnly Monday = SchedulingTestStore.NextMonday();

	private static TimeSpan Time(int hour, int minute = 0) => new(hour, minute, 0);

	private static UpdateDoctorScheduleRequest Schedule(
		int durationMinutes,
		params (DayOfWeek Day, TimeSpan Start, TimeSpan End)[] periods)
	{
		return new UpdateDoctorScheduleRequest
		{
			TimeZoneId = "UTC",
			ConsultationDurationMinutes = durationMinutes,
			Periods = periods
				.Select(period => new AvailabilityPeriodRequest
				{
					DayOfWeek = period.Day,
					StartTime = period.Start,
					EndTime = period.End,
				})
				.ToList(),
		};
	}

	private static SchedulingTestStore StoreWithActiveAppointment()
	{
		var store = new SchedulingTestStore();
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(17));
		store.AddAppointment(
			SchedulingTestStore.Utc(Monday, 9),
			TimeSpan.FromMinutes(30),
			AppointmentStatus.Pending);
		return store;
	}

	[Fact]
	public async Task schedule_change_is_rejected_when_it_invalidates_an_active_appointment()
	{
		using var store = StoreWithActiveAppointment();

		var result = await store.Service.UpdateScheduleAsync(
			store.Doctor.Id,
			Schedule(30, (DayOfWeek.Monday, Time(10), Time(17))));

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.AppointmentConflict");
		store.Context.DoctorAvailabilityPeriods
			.Should().ContainSingle()
			.Which.StartTime.Should().Be(Time(9));
	}

	[Fact]
	public async Task schedule_change_keeping_an_active_appointment_inside_a_period_is_saved()
	{
		using var store = StoreWithActiveAppointment();

		var result = await store.Service.UpdateScheduleAsync(
			store.Doctor.Id,
			Schedule(
				45,
				(DayOfWeek.Monday, Time(9), Time(12)),
				(DayOfWeek.Monday, Time(13), Time(17)),
				(DayOfWeek.Wednesday, Time(10), Time(14))));

		result.IsFailure.Should().BeFalse(result.Error.Description);
		result.Value.ConsultationDurationMinutes.Should().Be(45);
		result.Value.Periods.Should().HaveCount(3);
		store.Context.DoctorAvailabilityPeriods.Count().Should().Be(3);
		store.Context.DoctorSchedulingSettings.Single()
			.ConsultationDurationMinutes.Should().Be(45);
	}

	[Fact]
	public async Task overlapping_periods_are_rejected()
	{
		using var store = new SchedulingTestStore();

		var result = await store.Service.UpdateScheduleAsync(
			store.Doctor.Id,
			Schedule(
				30,
				(DayOfWeek.Monday, Time(9), Time(12)),
				(DayOfWeek.Monday, Time(11), Time(13))));

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.OverlappingPeriods");
	}

	[Fact]
	public async Task another_doctor_cannot_edit_the_schedule()
	{
		using var store = new SchedulingTestStore();

		var result = await store.ServiceFor("doctor-2").UpdateScheduleAsync(
			store.Doctor.Id,
			Schedule(30, (DayOfWeek.Monday, Time(9), Time(17))));

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.Forbidden");
	}

	[Fact]
	public async Task time_off_is_rejected_when_it_would_invalidate_an_active_appointment()
	{
		using var store = StoreWithActiveAppointment();

		var result = await store.Service.CreateTimeOffAsync(
			store.Doctor.Id,
			new CreateTimeOffRequest
			{
				Date = Monday,
				StartTime = Time(8),
				EndTime = Time(10),
				Reason = "Conference",
			});

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.AppointmentConflict");
		store.Context.DoctorTimeOff.Should().BeEmpty();
	}

	[Fact]
	public async Task overlapping_time_off_is_rejected()
	{
		using var store = new SchedulingTestStore();

		var first = await store.Service.CreateTimeOffAsync(
			store.Doctor.Id,
			new CreateTimeOffRequest
			{
				Date = Monday,
				StartTime = Time(8),
				EndTime = Time(9),
				IsFullDay = false,
			});
		first.IsFailure.Should().BeFalse(first.Error.Description);

		var second = await store.Service.CreateTimeOffAsync(
			store.Doctor.Id,
			new CreateTimeOffRequest
			{
				Date = Monday,
				StartTime = Time(8, 30),
				EndTime = Time(9, 30),
				IsFullDay = false,
			});

		second.IsFailure.Should().BeTrue();
		second.Error.Code.Should().Be("Scheduling.TimeOffOverlap");
	}

	[Fact]
	public async Task full_day_time_off_uses_the_doctor_local_date()
	{
		using var store = new SchedulingTestStore(timeZoneId: "Asia/Riyadh");

		var result = await store.Service.CreateTimeOffAsync(
			store.Doctor.Id,
			new CreateTimeOffRequest
			{
				Date = Monday,
				IsFullDay = true,
				Reason = "Public holiday",
			});

		result.IsFailure.Should().BeFalse(result.Error.Description);
		result.Value.Date.Should().Be(Monday);
		result.Value.IsFullDay.Should().BeTrue();
		// Local midnight Monday in Riyadh is 21:00 Sunday UTC.
		result.Value.StartTimeUtc.Should().Be(
			SchedulingTestStore.Utc(Monday.AddDays(-1), 21));
		result.Value.EndDate.Should().Be(Monday);
	}

	[Fact]
	public async Task full_day_time_off_range_blocks_every_date_inclusive()
	{
		using var store = new SchedulingTestStore();
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(10));
		store.AddPeriod(DayOfWeek.Tuesday, Time(9), Time(10));
		store.AddPeriod(DayOfWeek.Wednesday, Time(9), Time(10));

		var result = await store.Service.CreateTimeOffAsync(
			store.Doctor.Id,
			new CreateTimeOffRequest
			{
				Date = Monday,
				EndDate = Monday.AddDays(1),
				IsFullDay = true,
				Reason = "Two-day holiday",
			});

		result.IsFailure.Should().BeFalse(result.Error.Description);
		result.Value.Date.Should().Be(Monday);
		result.Value.EndDate.Should().Be(Monday.AddDays(1));
		result.Value.IsFullDay.Should().BeTrue();
		result.Value.EndTimeUtc.Should().Be(SchedulingTestStore.Utc(Monday.AddDays(2), 0));

		var availability = await store.Service.GetAvailabilityAsync(
			store.Doctor.Id,
			Monday,
			Monday.AddDays(2));

		availability.IsFailure.Should().BeFalse(availability.Error.Description);
		availability.Value.Days[0].Slots.Should().BeEmpty();
		availability.Value.Days[1].Slots.Should().BeEmpty();
		availability.Value.Days[2].Slots.Should().NotBeEmpty();
	}

	[Fact]
	public async Task partial_time_off_range_may_end_at_an_earlier_clock_time_on_a_later_date()
	{
		using var store = new SchedulingTestStore();
		store.AddPeriod(DayOfWeek.Monday, Time(16), Time(18));
		store.AddPeriod(DayOfWeek.Tuesday, Time(8), Time(10));
		store.AddPeriod(DayOfWeek.Wednesday, Time(8), Time(10));

		var result = await store.Service.CreateTimeOffAsync(
			store.Doctor.Id,
			new CreateTimeOffRequest
			{
				Date = Monday,
				EndDate = Monday.AddDays(2),
				StartTime = Time(17),
				EndTime = Time(9),
				IsFullDay = false,
			});

		result.IsFailure.Should().BeFalse(result.Error.Description);
		result.Value.Date.Should().Be(Monday);
		result.Value.EndDate.Should().Be(Monday.AddDays(2));
		result.Value.IsFullDay.Should().BeFalse();
		result.Value.StartTime.Should().Be(Time(17));
		result.Value.EndTime.Should().Be(Time(9));
		result.Value.StartTimeUtc.Should().Be(SchedulingTestStore.Utc(Monday, 17));
		result.Value.EndTimeUtc.Should().Be(SchedulingTestStore.Utc(Monday.AddDays(2), 9));

		var availability = await store.Service.GetAvailabilityAsync(
			store.Doctor.Id,
			Monday,
			Monday.AddDays(2));

		availability.IsFailure.Should().BeFalse(availability.Error.Description);
		availability.Value.Days[0].Slots.Select(slot => slot.StartTime).Should().Equal(
			SchedulingTestStore.Utc(Monday, 16),
			SchedulingTestStore.Utc(Monday, 16, 30));
		availability.Value.Days[1].Slots.Should().BeEmpty();
		availability.Value.Days[2].Slots.Select(slot => slot.StartTime).Should().Equal(
			SchedulingTestStore.Utc(Monday.AddDays(2), 9),
			SchedulingTestStore.Utc(Monday.AddDays(2), 9, 30));
	}

	[Fact]
	public async Task time_off_range_rejects_an_end_date_before_the_start_date()
	{
		using var store = new SchedulingTestStore();

		var result = await store.Service.CreateTimeOffAsync(
			store.Doctor.Id,
			new CreateTimeOffRequest
			{
				Date = Monday,
				EndDate = Monday.AddDays(-1),
				IsFullDay = true,
			});

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Scheduling.InvalidRange");
		store.Context.DoctorTimeOff.Should().BeEmpty();
	}

	[Fact]
	public async Task deleting_time_off_restores_the_slots()
	{
		using var store = new SchedulingTestStore();
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(10));

		var created = await store.Service.CreateTimeOffAsync(
			store.Doctor.Id,
			new CreateTimeOffRequest
			{
				Date = Monday,
				StartTime = Time(9),
				EndTime = Time(10),
				IsFullDay = false,
			});
		created.IsFailure.Should().BeFalse(created.Error.Description);

		var before = await store.Service.GetAvailabilityAsync(store.Doctor.Id, Monday, Monday);
		before.Value.Days.Single().Slots.Should().BeEmpty();

		var deleted = await store.Service.DeleteTimeOffAsync(store.Doctor.Id, created.Value.Id);
		deleted.IsFailure.Should().BeFalse(deleted.Error.Description);

		var after = await store.Service.GetAvailabilityAsync(store.Doctor.Id, Monday, Monday);
		after.Value.Days.Single().Slots.Should().HaveCount(2);
	}
}
