using Application.Contracts;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Shared.RequestParameters;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Patient history is a merged projection over existing appointments, notes and
/// follow-ups. These tests pin the privacy boundary: a patient sees only their
/// own appointments and explicitly shared records, while a doctor sees only
/// records connected to them.
/// </summary>
public class PatientHistoryTests : IDisposable
{
	private const string DoctorOne = "doctor-1";
	private const string DoctorTwo = "doctor-2";
	private const string DoctorThree = "doctor-3";
	private const string PatientOne = "patient-1";
	private const string PatientTwo = "patient-2";

	private readonly AppDbContext _context;

	public PatientHistoryTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"history-{Guid.NewGuid()}")
			.Options;
		_context = new AppDbContext(options);
		Seed();
	}

	public void Dispose() => _context.Dispose();

	[Fact]
	public async Task Patient_sees_own_appointments_and_only_shared_notes_and_follow_ups()
	{
		var history = await CreateService(PatientOne)
			.GetMyHistoryAsync(1, 20);

		history.IsSuccess.Should().BeTrue();
		history.Value.PatientId.Should().Be(PatientOne);
		history.Value.Items.Should().Contain(item => item.Type == "appointment");
		history.Value.Items.Should().Contain(item => item.Type == "note" && item.Title == "Shared note");
		history.Value.Items.Should().Contain(item => item.Type == "follow_up" && item.Title.Contains("Shared follow-up"));
		history.Value.Items.Should().NotContain(item => item.Title == "Private note");
		history.Value.Items.Should().NotContain(item => item.Title.Contains("Private follow-up"));
	}

	[Fact]
	public async Task Doctor_sees_only_their_own_relationship_records_for_a_patient()
	{
		var history = await CreateService(DoctorOne)
			.GetPatientHistoryAsync(PatientOne, 1, 20);

		history.IsSuccess.Should().BeTrue();
		history.Value.PatientName.Should().Be("Pat One");
		history.Value.Items.Should().Contain(item => item.Type == "appointment" && item.DoctorId == DoctorOne);
		history.Value.Items.Should().NotContain(item => item.DoctorId == DoctorTwo);
		history.Value.Items.Should().Contain(item => item.Title == "Private note");
		history.Value.Items.Should().Contain(item => item.Title == "Shared note");
		history.Value.Items.Should().Contain(item => item.Title.Contains("Private follow-up"));
	}

	[Fact]
	public async Task Doctor_without_a_relationship_gets_no_history()
	{
		var history = await CreateService(DoctorThree)
			.GetPatientHistoryAsync(PatientOne, 1, 20);

		history.IsFailure.Should().BeTrue();
		history.Error.Should().Be(HistoryErrors.NotFound());
	}

	[Fact]
	public async Task Doctor_can_open_history_for_a_patient_with_a_record_but_not_another_doctor()
	{
		var allowed = await CreateService(DoctorTwo)
			.GetPatientHistoryAsync(PatientOne, 1, 20);

		allowed.IsSuccess.Should().BeTrue();
		allowed.Value.Items.Should().Contain(item => item.DoctorId == DoctorTwo);
		allowed.Value.Items.Should().NotContain(item => item.Title == "Private note");
	}

	private PatientHistoryService CreateService(string userId) =>
		new(
			new RepositoryManager(_context),
			new FakeClaims(userId),
			new FakePatientLookup());

	private void Seed()
	{
		_context.Doctors.AddRange(
			MakeDoctor(DoctorOne, "Dana", "One"),
			MakeDoctor(DoctorTwo, "Drew", "Two"),
			MakeDoctor(DoctorThree, "Dee", "Three"));
		_context.Clients.AddRange(
			MakePatient(PatientOne, "Pat", "One"),
			MakePatient(PatientTwo, "Pat", "Two"));
		_context.SaveChanges();

		_context.Appointments.AddRange(
			MakeAppointment(1, PatientOne, DoctorOne, AppointmentStatus.Completed, DateTime.UtcNow.AddDays(-3)),
			MakeAppointment(2, PatientOne, DoctorTwo, AppointmentStatus.Confirmed, DateTime.UtcNow.AddDays(-2)),
			MakeAppointment(3, PatientTwo, DoctorOne, AppointmentStatus.Completed, DateTime.UtcNow.AddDays(-1)));

		_context.PatientNotes.AddRange(
			new PatientNote
			{
				DoctorId = DoctorOne,
				PatientId = PatientOne,
				Title = "Private note",
				Body = "Internal only",
				CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
				IsPatientVisible = false,
			},
			new PatientNote
			{
				DoctorId = DoctorOne,
				PatientId = PatientOne,
				Title = "Shared note",
				Body = "Please review",
				CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
				IsPatientVisible = true,
			},
			new PatientNote
			{
				DoctorId = DoctorTwo,
				PatientId = PatientOne,
				Title = "Other doctor note",
				Body = "Private to doctor two",
				CreatedAtUtc = DateTime.UtcNow,
				IsPatientVisible = true,
			});

		_context.FollowUps.AddRange(
			new FollowUp
			{
				DoctorId = DoctorOne,
				PatientId = PatientOne,
				Reason = "Private follow-up",
				Status = FollowUpStatus.Open,
				CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
				IsPatientVisible = false,
			},
			new FollowUp
			{
				DoctorId = DoctorOne,
				PatientId = PatientOne,
				Reason = "Shared follow-up",
				Status = FollowUpStatus.Open,
				CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
				IsPatientVisible = true,
			});
		_context.SaveChanges();
	}

	private static Doctor MakeDoctor(string id, string firstName, string lastName) =>
		new()
		{
			Id = id,
			Email = $"{id}@example.com",
			UserName = id,
			NormalizedEmail = $"{id}@EXAMPLE.COM",
			NormalizedUserName = id.ToUpperInvariant(),
			FirstName = firstName,
			LastName = lastName,
			Gender = "female",
			IsAvailable = true,
			SessionFees = 100m,
			Specialization = DoctorSpecialization.ClinicalPsychology,
		};

	private static User MakePatient(string id, string firstName, string lastName) =>
		new()
		{
			Id = id,
			Email = $"{id}@example.com",
			UserName = id,
			NormalizedEmail = $"{id}@EXAMPLE.COM",
			NormalizedUserName = id.ToUpperInvariant(),
			FirstName = firstName,
			LastName = lastName,
			Gender = "female",
		};

	private static Appointment MakeAppointment(
		int id,
		string patientId,
		string doctorId,
		AppointmentStatus status,
		DateTime startTime) =>
		new()
		{
			Id = id,
			UserId = patientId,
			DoctorId = doctorId,
			StartTime = startTime,
			Duration = TimeSpan.FromMinutes(30),
			Status = status,
			Location = "Nexus Clinic",
			Fees = 100m,
		};

	private sealed class FakeClaims : IUserClaimsService
	{
		private readonly string _userId;
		public FakeClaims(string userId) => _userId = userId;
		public string GetUserId() => _userId;
		public string GetUserName() => "Test User";
		public string GetRole() => "User";
		public string GetPhotoUrl() => string.Empty;
		public string GetEmail() => "test@example.com";
	}

	private sealed class FakePatientLookup : IPatientDirectoryLookup
	{
		private static readonly Dictionary<string, string> Names = new()
		{
			[PatientOne] = "Pat One",
			[PatientTwo] = "Pat Two",
		};

		public Task<IReadOnlyDictionary<string, string>> GetDisplayNamesAsync(
			IReadOnlyCollection<string> userIds) =>
			Task.FromResult<IReadOnlyDictionary<string, string>>(
				userIds.Where(Names.ContainsKey).ToDictionary(id => id, id => Names[id]));

		public Task<bool> ExistsAsync(string userId) =>
			Task.FromResult(Names.ContainsKey(userId));
	}
}
