using Application.Services;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// The dashboard's window rules are the part that is easy to get quietly wrong
/// (cancelled rows counted as patients, old completions averaged into today's
/// duration), so they are asserted against real repositories over a seeded
/// in-memory store rather than against a mocked aggregate.
/// </summary>
public class DoctorWorkspaceMetricsTests : IDisposable
{
	private const string DoctorId = "doctor-1";
	private const string OtherDoctorId = "doctor-2";

	private readonly AppDbContext _context;
	private readonly DoctorWorkspaceService _service;
	private readonly FakeNotificationSender _notifications;

	public DoctorWorkspaceMetricsTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"workspace-{Guid.NewGuid()}")
			.Options;

		_context = new AppDbContext(options);
		_notifications = new FakeNotificationSender(_context);
		_service = CreateService(_context, DoctorId, _notifications);
	}

	public void Dispose() => _context.Dispose();

	private static DoctorWorkspaceService CreateService(
		AppDbContext context,
		string doctorId,
		FakeNotificationSender? notificationSender = null) =>
		new(
			new RepositoryManager(context),
			new TestUserClaimsService(doctorId),
			new PatientDirectoryLookup(context),
			notificationSender ?? new FakeNotificationSender(context));

	private void AddPatient(string id, string firstName, string lastName)
	{
		_context.Clients.Add(new User
		{
			Id = id,
			Email = $"{id}@example.com",
			UserName = id,
			NormalizedEmail = $"{id}@EXAMPLE.COM",
			NormalizedUserName = id.ToUpperInvariant(),
			FirstName = firstName,
			LastName = lastName,
			Gender = "other",
		});
	}

	private Appointment AddAppointment(
		string patientId,
		AppointmentStatus status,
		DateTime startTime,
		int durationMinutes = 30,
		string doctorId = DoctorId)
	{
		var appointment = new Appointment
		{
			UserId = patientId,
			DoctorId = doctorId,
			StartTime = startTime,
			Duration = TimeSpan.FromMinutes(durationMinutes),
			Status = status,
			Location = "Online",
			Fees = 100m,
		};

		_context.Appointments.Add(appointment);
		return appointment;
	}

	private ChatRoom AddRoom(string name, DateTime createdAtUtc, DateTime? lastMessageAtUtc = null)
	{
		var room = new ChatRoom
		{
			Name = name,
			Description = string.Empty,
			Visibility = ChatRoomVisibility.Public,
			CreatedById = DoctorId,
			CreatedAtUtc = createdAtUtc,
			LastMessageAtUtc = lastMessageAtUtc,
		};

		_context.ChatRooms.Add(room);
		return room;
	}

	[Fact]
	public async Task Dashboard_windows_exclude_stale_cancelled_and_rejected_rows()
	{
		var now = DateTime.UtcNow;

		AddAppointment("p1", AppointmentStatus.Confirmed, now.AddHours(-1));
		AddAppointment("p1", AppointmentStatus.Confirmed, now.AddDays(2));
		AddAppointment("p2", AppointmentStatus.Pending, now.AddDays(1));
		AddAppointment("p2", AppointmentStatus.Completed, now.AddDays(-5), 60);
		AddAppointment("p3", AppointmentStatus.Confirmed, now.AddDays(-10));
		AddAppointment("p4", AppointmentStatus.Cancelled, now.AddDays(-3));
		AddAppointment("p5", AppointmentStatus.Rejected, now.AddDays(1));
		AddAppointment("p6", AppointmentStatus.Confirmed, now.AddDays(-9), doctorId: OtherDoctorId);

		await _context.SaveChangesAsync();

		var result = await _service.GetDashboardAsync();

		result.IsSuccess.Should().BeTrue();
		result.Value.ActiveConsultations.Should().Be(1);
		result.Value.UpcomingConsultations.Should().Be(1);
		result.Value.PendingPatientRequests.Should().Be(1);

		// p1, p2 and p3; the cancelled (p4), rejected (p5) and other doctor's
		// (p6) rows must not make a patient "active".
		result.Value.DistinctActivePatients.Should().Be(3);
	}

	[Fact]
	public async Task The_average_uses_only_completed_scheduled_durations_in_the_window()
	{
		var now = DateTime.UtcNow;

		AddAppointment("p1", AppointmentStatus.Completed, now.AddDays(-5), 60);
		AddAppointment("p2", AppointmentStatus.Completed, now.AddDays(-200), 10);
		AddAppointment("p3", AppointmentStatus.Confirmed, now.AddDays(-3), 45);

		await _context.SaveChangesAsync();

		var result = await _service.GetDashboardAsync();

		result.Value.AverageConsultationDurationMinutes.Should().Be(60);
		result.Value.AverageConsultationWindowDays.Should().Be(90);
	}

	[Fact]
	public async Task A_dashboard_with_no_completed_consultations_reports_no_average()
	{
		AddAppointment("p1", AppointmentStatus.Confirmed, DateTime.UtcNow.AddDays(1));
		await _context.SaveChangesAsync();

		var result = await _service.GetDashboardAsync();

		result.Value.AverageConsultationDurationMinutes.Should().BeNull();
	}

	[Fact]
	public async Task Room_counts_separate_hosted_from_open()
	{
		var now = DateTime.UtcNow;
		AddRoom("Open room", now.AddDays(-2), now.AddMinutes(-10));
		AddRoom("Archived room", now.AddDays(-3));
		var archived = _context.ChatRooms.Local.Single(room => room.Name == "Archived room");
		archived.IsArchived = true;

		await _context.SaveChangesAsync();

		var result = await _service.GetDashboardAsync();

		result.Value.TotalRoomsHosted.Should().Be(2);
		result.Value.ActiveRoomsHosted.Should().Be(1);
		result.Value.LiveRooms.Should().ContainSingle(room => room.Name == "Open room");
	}

	[Fact]
	public async Task A_note_is_scoped_to_the_doctor_who_wrote_it()
	{
		AddPatient("p1", "Alice", "Patient");
		await _context.SaveChangesAsync();

		var created = await _service.CreateNoteAsync(new Application.Dtos.DoctorWorkspaceDtos.CreatePatientNoteRequest
		{
			PatientId = "p1",
			Title = "Session summary",
			Body = "Patient reported improvement.",
		});

		created.IsSuccess.Should().BeTrue();

		var otherDoctorService = CreateService(_context, OtherDoctorId);
		var update = await otherDoctorService.UpdateNoteAsync(
			created.Value.Id,
			new Application.Dtos.DoctorWorkspaceDtos.UpdatePatientNoteRequest { Title = "Hijacked" });

		update.IsFailure.Should().BeTrue();
		update.Error.Code.Should().Be("Note.NotFound");

		var notes = await otherDoctorService.GetNotesAsync(null, 20);
		notes.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task A_follow_up_must_name_a_real_patient()
	{
		var result = await _service.CreateFollowUpAsync(new Application.Dtos.DoctorWorkspaceDtos.CreateFollowUpRequest
		{
			PatientId = "missing",
			Reason = "Check in",
		});

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Workspace.PatientNotFound");
	}

	[Fact]
	public async Task Resolving_a_follow_up_stamps_the_resolution_time_and_reopening_clears_it()
	{
		AddPatient("p1", "Alice", "Patient");
		await _context.SaveChangesAsync();

		var created = await _service.CreateFollowUpAsync(new Application.Dtos.DoctorWorkspaceDtos.CreateFollowUpRequest
		{
			PatientId = "p1",
			Reason = "Call in two weeks",
		});
		created.IsSuccess.Should().BeTrue();
		created.Value.PatientName.Should().Be("Alice Patient");

		var done = await _service.UpdateFollowUpAsync(
			created.Value.Id,
			new Application.Dtos.DoctorWorkspaceDtos.UpdateFollowUpRequest { Status = FollowUpStatus.Done });

		done.Value.Status.Should().Be(FollowUpStatus.Done);
		done.Value.ResolvedAtUtc.Should().NotBeNull();

		var reopened = await _service.UpdateFollowUpAsync(
			created.Value.Id,
			new Application.Dtos.DoctorWorkspaceDtos.UpdateFollowUpRequest { Status = FollowUpStatus.Open });

		reopened.Value.Status.Should().Be(FollowUpStatus.Open);
		reopened.Value.ResolvedAtUtc.Should().BeNull();
	}

	[Fact]
	public async Task Sharing_a_note_notifies_the_patient_once_without_clinical_content()
	{
		AddPatient("p1", "Alice", "Patient");
		await _context.SaveChangesAsync();

		var created = await _service.CreateNoteAsync(new Application.Dtos.DoctorWorkspaceDtos.CreatePatientNoteRequest
		{
			PatientId = "p1",
			Title = "Session summary",
			Body = "Patient reported improvement.",
			IsPatientVisible = false,
		});
		created.IsSuccess.Should().BeTrue();
		_notifications.Sent.Should().BeEmpty();

		var shared = await _service.UpdateNoteAsync(
			created.Value.Id,
			new Application.Dtos.DoctorWorkspaceDtos.UpdatePatientNoteRequest { IsPatientVisible = true });
		shared.IsSuccess.Should().BeTrue();

		_notifications.Sent.Should().ContainSingle();
		var notification = _notifications.Sent[0];
		notification.AppUserId.Should().Be("p1");
		notification.Type.Should().Be(nameof(NotificationType.PatientNote));
		notification.Message.Should().NotContain("Session summary");
		notification.Message.Should().NotContain("improvement");

		await _service.UpdateNoteAsync(
			created.Value.Id,
			new Application.Dtos.DoctorWorkspaceDtos.UpdatePatientNoteRequest { IsPatientVisible = false });
		await _service.UpdateNoteAsync(
			created.Value.Id,
			new Application.Dtos.DoctorWorkspaceDtos.UpdatePatientNoteRequest { IsPatientVisible = true });
		_notifications.Sent.Should().ContainSingle();
	}

	[Fact]
	public async Task Sharing_a_follow_up_notifies_the_patient_and_status_changes_do_not()
	{
		AddPatient("p1", "Alice", "Patient");
		await _context.SaveChangesAsync();

		var created = await _service.CreateFollowUpAsync(new Application.Dtos.DoctorWorkspaceDtos.CreateFollowUpRequest
		{
			PatientId = "p1",
			Reason = "Check in",
			IsPatientVisible = true,
		});
		created.IsSuccess.Should().BeTrue();

		_notifications.Sent.Should().ContainSingle();
		_notifications.Sent[0].Type.Should().Be(nameof(NotificationType.FollowUp));
		_notifications.Sent[0].AppUserId.Should().Be("p1");
		_notifications.Sent[0].Message.Should().NotContain("Check in");

		await _service.UpdateFollowUpAsync(
			created.Value.Id,
			new Application.Dtos.DoctorWorkspaceDtos.UpdateFollowUpRequest { Status = FollowUpStatus.Done });
		_notifications.Sent.Should().ContainSingle();
	}

	[Fact]
	public async Task The_activity_feed_merges_joins_notes_and_due_follow_ups_newest_first()
	{
		var now = DateTime.UtcNow;
		AddPatient("p1", "Alice", "Patient");
		AddPatient("p2", "Bob", "Patient");
		AddPatient("p3", "Carol", "Patient");

		var room = AddRoom("Support circle", now.AddDays(-5), now.AddMinutes(-5));
		await _context.SaveChangesAsync();

		_context.ChatRoomMembers.Add(new ChatRoomMember
		{
			RoomId = room.Id,
			UserId = "p3",
			Role = ChatRoomRole.Member,
			DisplayName = "Patient A1B2C3",
			IsDoctor = false,
			JoinedAtUtc = now.AddMinutes(-30),
		});
		_context.PatientNotes.Add(new PatientNote
		{
			DoctorId = DoctorId,
			PatientId = "p1",
			Title = "Session summary",
			Body = "Stable.",
			CreatedAtUtc = now.AddHours(-1),
		});
		_context.FollowUps.Add(new FollowUp
		{
			DoctorId = DoctorId,
			PatientId = "p2",
			Reason = "Check medication",
			Status = FollowUpStatus.Open,
			DueAtUtc = now.AddHours(-2),
			CreatedAtUtc = now.AddDays(-1),
		});
		await _context.SaveChangesAsync();

		var result = await _service.GetActivityAsync(10);

		result.IsSuccess.Should().BeTrue();
		var feed = result.Value;
		feed.Should().HaveCount(3);
		feed[0].Type.Should().Be("room_join");
		feed[0].PatientName.Should().Be("Patient A1B2C3");
		feed[1].Type.Should().Be("note_added");
		feed[1].PatientName.Should().Be("Alice Patient");
		feed[2].Type.Should().Be("follow_up_due");
		feed[2].PatientName.Should().Be("Bob Patient");
	}
}
