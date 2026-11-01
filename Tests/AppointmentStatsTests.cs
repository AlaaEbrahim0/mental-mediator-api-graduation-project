using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Pins two data-access invariants that a prior audit flagged and that are easy
/// to silently regress: the doctor "profit" scalar must count only completed
/// appointments, and the repository tracking flag must actually control EF change
/// tracking (it was once inverted).
/// </summary>
public class AppointmentStatsTests : IDisposable
{
	private const string DoctorOne = "doctor-1";
	private const string DoctorTwo = "doctor-2";

	private readonly AppDbContext _context;

	public AppointmentStatsTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"appt-stats-{Guid.NewGuid()}")
			.Options;
		_context = new AppDbContext(options);
	}

	public void Dispose() => _context.Dispose();

	private void AddAppointment(string doctorId, AppointmentStatus status, decimal fees)
	{
		_context.Appointments.Add(new Appointment
		{
			UserId = "patient-1",
			DoctorId = doctorId,
			StartTime = new DateTime(2026, 5, 1, 10, 0, 0, DateTimeKind.Utc),
			Duration = TimeSpan.FromMinutes(45),
			Status = status,
			Location = "Online",
			Fees = fees,
		});
		_context.SaveChanges();
	}

	[Fact]
	public async Task GetDoctorStats_profit_counts_only_completed_appointments()
	{
		AddAppointment(DoctorOne, AppointmentStatus.Completed, 100m);
		AddAppointment(DoctorOne, AppointmentStatus.Completed, 50m);
		AddAppointment(DoctorOne, AppointmentStatus.Cancelled, 999m);
		AddAppointment(DoctorOne, AppointmentStatus.Rejected, 999m);
		AddAppointment(DoctorOne, AppointmentStatus.Pending, 999m);
		AddAppointment(DoctorTwo, AppointmentStatus.Completed, 777m);

		var repo = new AppointmentRepository(_context);
		var (totalAppointments, totalProfit) = await repo.GetDoctorStats(DoctorOne);

		// Revenue is the two completed rows only; cancelled/rejected/pending and
		// the other doctor's fees must not leak in.
		totalProfit.Should().Be(150m);
		// NOTE: the current implementation also counts only completed rows here.
		// Whether "TotalAppointments" should instead be every status (the report
		// also shows a full status breakdown) is an open product question; this
		// assertion pins today's behavior so a change is deliberate.
		totalAppointments.Should().Be(2);
	}

	[Fact]
	public async Task FindAll_tracks_only_when_trackChanges_is_true()
	{
		AddAppointment(DoctorOne, AppointmentStatus.Completed, 10m);

		var repo = new AppointmentRepository(_context);

		_context.ChangeTracker.Clear();
		var untracked = await repo.FindAll(trackChanges: false).ToListAsync();
		untracked.Should().HaveCount(1);
		_context.ChangeTracker.Entries<Appointment>().Should().BeEmpty();

		_context.ChangeTracker.Clear();
		var tracked = await repo.FindAll(trackChanges: true).ToListAsync();
		tracked.Should().HaveCount(1);
		_context.ChangeTracker.Entries<Appointment>().Should().NotBeEmpty();
	}

	[Fact]
	public async Task FindByCondition_respects_the_trackChanges_flag()
	{
		AddAppointment(DoctorOne, AppointmentStatus.Completed, 10m);
		AddAppointment(DoctorTwo, AppointmentStatus.Completed, 10m);

		var repo = new AppointmentRepository(_context);

		_context.ChangeTracker.Clear();
		var filtered = await repo
			.FindByCondition(a => a.DoctorId == DoctorOne, trackChanges: false)
			.ToListAsync();

		filtered.Should().HaveCount(1);
		_context.ChangeTracker.Entries<Appointment>().Should().BeEmpty();
	}
}
