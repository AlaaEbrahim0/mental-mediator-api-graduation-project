using Application.Dtos.SchedulingDtos;
using Domain.Enums;
using FluentAssertions;
using Xunit;

namespace MentalMediator.Tests;

public class SlotCalculationTests
{
	private static readonly DateOnly Monday = SchedulingTestStore.NextMonday();

	private static TimeSpan Time(int hour, int minute = 0) => new(hour, minute, 0);

	private static async Task<List<AvailableSlotResponse>> Slots(
		SchedulingTestStore store,
		DateOnly? date = null)
	{
		var result = await store.Service.GetAvailabilityAsync(
			store.Doctor.Id,
			date ?? Monday,
			date ?? Monday);

		result.IsFailure.Should().BeFalse(result.Error.Description);
		return result.Value.Days.Single().Slots;
	}

	[Fact]
	public async Task multiple_periods_on_the_same_day_leave_a_gap()
	{
		using var store = new SchedulingTestStore();
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(10));
		store.AddPeriod(DayOfWeek.Monday, Time(10, 30), Time(11, 30));

		var slots = await Slots(store);

		slots.Select(slot => slot.StartTime).Should().Equal(
			SchedulingTestStore.Utc(Monday, 9),
			SchedulingTestStore.Utc(Monday, 9, 30),
			SchedulingTestStore.Utc(Monday, 10, 30),
			SchedulingTestStore.Utc(Monday, 11));
		slots.Should().OnlyContain(slot => slot.DurationMinutes == 30);
	}

	[Fact]
	public async Task slots_never_cross_a_period_boundary()
	{
		using var store = new SchedulingTestStore(consultationDurationMinutes: 45);
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(10, 30));

		var slots = await Slots(store);

		slots.Should().HaveCount(2);
		slots.Select(slot => slot.StartTime).Should().Equal(
			SchedulingTestStore.Utc(Monday, 9),
			SchedulingTestStore.Utc(Monday, 9, 45));
		slots.Last().EndTime.Should().Be(SchedulingTestStore.Utc(Monday, 10, 30));
	}

	[Fact]
	public async Task a_period_shorter_than_the_duration_offers_nothing()
	{
		using var store = new SchedulingTestStore(consultationDurationMinutes: 60);
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(9, 45));

		var slots = await Slots(store);

		slots.Should().BeEmpty();
	}

	[Fact]
	public async Task time_off_removes_only_the_slots_it_overlaps()
	{
		using var store = new SchedulingTestStore();
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(11));
		store.AddTimeOff(
			SchedulingTestStore.Utc(Monday, 9, 30),
			SchedulingTestStore.Utc(Monday, 10, 30));

		var slots = await Slots(store);

		// 09:00-09:30 touches the time off and remains bookable.
		slots.Select(slot => slot.StartTime).Should().Equal(
			SchedulingTestStore.Utc(Monday, 9),
			SchedulingTestStore.Utc(Monday, 10, 30));
	}

	[Fact]
	public async Task pending_and_confirmed_appointments_reserve_their_slot()
	{
		using var store = new SchedulingTestStore();
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(10));
		store.AddAppointment(SchedulingTestStore.Utc(Monday, 9), TimeSpan.FromMinutes(30));
		store.AddAppointment(
			SchedulingTestStore.Utc(Monday, 9, 30),
			TimeSpan.FromMinutes(30),
			AppointmentStatus.Pending);

		var slots = await Slots(store);

		slots.Should().BeEmpty();
	}

	[Fact]
	public async Task cancelled_and_rejected_appointments_do_not_reserve_a_slot()
	{
		using var store = new SchedulingTestStore();
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(10));
		store.AddAppointment(
			SchedulingTestStore.Utc(Monday, 9),
			TimeSpan.FromMinutes(30),
			AppointmentStatus.Cancelled);
		store.AddAppointment(
			SchedulingTestStore.Utc(Monday, 9, 30),
			TimeSpan.FromMinutes(30),
			AppointmentStatus.Rejected);

		var slots = await Slots(store);

		slots.Should().HaveCount(2);
	}

	[Fact]
	public async Task weekly_periods_are_interpreted_in_the_doctor_time_zone()
	{
		using var store = new SchedulingTestStore(timeZoneId: "Asia/Riyadh");
		store.AddPeriod(DayOfWeek.Monday, Time(9), Time(10));

		var result = await store.Service.GetAvailabilityAsync(store.Doctor.Id, Monday, Monday);
		result.IsFailure.Should().BeFalse(result.Error.Description);

		// Riyadh is UTC+3; the first local slot is 06:00 UTC.
		result.Value.TimeZoneId.Should().Be("Asia/Riyadh");
		result.Value.Days.Single().Slots.First().StartTime
			.Should().Be(SchedulingTestStore.Utc(Monday, 6));
	}

	[Fact]
	public async Task empty_weekly_configuration_offers_nothing()
	{
		using var store = new SchedulingTestStore();

		var slots = await Slots(store);

		slots.Should().BeEmpty();
	}
}
