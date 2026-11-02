using Application.Contracts;
using Application.Dtos.DoctorRatings;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Shared;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// One row per (doctor, rater) is the whole rule: a second submission edits
/// the existing vote, deletion is keyed by the caller so another patient's
/// row can never be reached, and the average must not truncate.
/// </summary>
public class DoctorRatingTests : IDisposable
{
	private const string DoctorId = "doctor-1";
	private const string PatientId = "patient-1";
	private const string OtherPatientId = "patient-2";

	private readonly AppDbContext _context;

	public DoctorRatingTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"ratings-{Guid.NewGuid()}")
			.Options;

		_context = new AppDbContext(options);
		_context.Doctors.Add(new Doctor
		{
			Id = DoctorId,
			Email = "doctor@example.com",
			UserName = "doctor",
			NormalizedEmail = "DOCTOR@EXAMPLE.COM",
			NormalizedUserName = "DOCTOR",
			FirstName = "Dana",
			LastName = "Doctor",
			Gender = "female",
			IsAvailable = true,
			SessionFees = 100m,
		});
		_context.SaveChanges();
	}

	public void Dispose() => _context.Dispose();

	private DoctorRatingService CreateService(string userId, string role, IHateSpeechDetector? detector = null) =>
		new(
			new RepositoryManager(_context),
			new FakeClaims(userId, role),
			detector ?? new FakeHateSpeechDetector(false),
			NullLogger<DoctorRatingService>.Instance);

	[Fact]
	public async Task Upsert_creates_one_row_with_the_raters_display_name()
	{
		var service = CreateService(PatientId, "User");

		var result = await service.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 5, Comment = " Kind " });

		result.IsFailure.Should().BeFalse();
		var rows = await _context.DoctorRatings.ToListAsync();
		rows.Should().ContainSingle();
		rows[0].Stars.Should().Be(5);
		rows[0].Comment.Should().Be("Kind");
		rows[0].RaterUserId.Should().Be(PatientId);
		rows[0].CreatedAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
		rows[0].UpdatedAtUtc.Should().BeNull();
	}

	[Fact]
	public async Task Second_upsert_edits_the_same_row_instead_of_adding_a_vote()
	{
		var service = CreateService(PatientId, "User");
		var first = await service.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 2 });

		var second = await service.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 4 });

		second.IsFailure.Should().BeFalse();
		second.Value.Id.Should().Be(first.Value.Id);
		second.Value.Stars.Should().Be(4);
		second.Value.UpdatedAtUtc.Should().NotBeNull();
		(await _context.DoctorRatings.AsNoTracking().ToListAsync()).Should().HaveCount(1);
	}

	[Theory]
	[InlineData("Doctor")]
	[InlineData("Admin")]
	public async Task Non_patient_roles_cannot_rate(string role)
	{
		var service = CreateService(DoctorId, role);

		var result = await service.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 5 });

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("DoctorRatings.PatientsOnly");
	}

	[Fact]
	public async Task A_doctor_rating_itself_is_refused()
	{
		var service = CreateService(DoctorId, "User");

		var result = await service.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 5 });

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("DoctorRatings.SelfRating");
	}

	[Fact]
	public async Task An_unknown_doctor_is_not_found()
	{
		var service = CreateService(PatientId, "User");

		var result = await service.UpsertAsync("ghost-doctor", new UpsertDoctorRatingRequest { Stars = 3 });

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("DoctorRatings.DoctorNotFound");
	}

	[Fact]
	public async Task Delete_removes_only_the_callers_own_row()
	{
		var mine = CreateService(PatientId, "User");
		var other = CreateService(OtherPatientId, "User");
		await mine.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 5 });

		(await other.DeleteAsync(DoctorId)).IsFailure.Should().BeTrue();
		(await _context.DoctorRatings.AsNoTracking().ToListAsync()).Should().HaveCount(1);

		(await mine.DeleteAsync(DoctorId)).IsFailure.Should().BeFalse();
		(await _context.DoctorRatings.AsNoTracking().ToListAsync()).Should().BeEmpty();

		var again = await mine.DeleteAsync(DoctorId);
		again.IsFailure.Should().BeTrue();
		again.Error.Code.Should().Be("DoctorRatings.NotRated");
	}

	[Fact]
	public async Task The_average_does_not_truncate_and_counts_survive_a_delete()
	{
		var a = CreateService(PatientId, "User");
		var b = CreateService(OtherPatientId, "User");
		await a.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 4 });
		await b.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 5 });

		var summary = await a.GetSummaryAsync(DoctorId);
		summary.Value.Average.Should().Be(4.5);
		summary.Value.Count.Should().Be(2);

		await b.DeleteAsync(DoctorId);
		summary = await a.GetSummaryAsync(DoctorId);
		summary.Value.Average.Should().Be(4);
		summary.Value.Count.Should().Be(1);
	}

	[Fact]
	public async Task Summaries_are_returned_only_for_the_requested_ids()
	{
		var service = CreateService(PatientId, "User");
		await service.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 3 });

		var summaries = await _context.Database.CreateExecutionStrategy()
			.ExecuteAsync(() => new DoctorRatingRepository(_context)
				.GetSummariesAsync(new[] { DoctorId, "unrated-doctor" }));

		summaries.Should().ContainKey(DoctorId);
		summaries.Should().NotContainKey("unrated-doctor");
		summaries[DoctorId].Average.Should().Be(3);
	}

	[Fact]
	public async Task A_hateful_comment_is_refused_but_a_detector_failure_allows_the_vote()
	{
		var blocking = CreateService(PatientId, "User", new FakeHateSpeechDetector(true));
		var refused = await blocking.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 1, Comment = "slur" });
		refused.IsFailure.Should().BeTrue();
		refused.Error.Code.Should().Be("Content.Forbidden");

		var failing = CreateService(OtherPatientId, "User", new FakeHateSpeechDetector(null));
		var allowed = await failing.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 1, Comment = "text" });
		allowed.IsFailure.Should().BeFalse();
	}

	[Fact]
	public async Task Recent_ratings_newest_first_and_mine_is_scoped_to_the_caller()
	{
		var a = CreateService(PatientId, "User");
		var b = CreateService(OtherPatientId, "User");
		var first = await a.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 3 });
		var second = await b.UpsertAsync(DoctorId, new UpsertDoctorRatingRequest { Stars = 5 });

		var recent = await a.GetRecentAsync(DoctorId);
		recent.Value.Should().HaveCount(2);
		recent.Value[0].Id.Should().Be(second.Value.Id);
		recent.Value[1].Id.Should().Be(first.Value.Id);

		var mine = await a.GetMineAsync(DoctorId);
		mine.Value.Stars.Should().Be(3);

		var nobody = CreateService("patient-3", "User");
		(await nobody.GetMineAsync(DoctorId)).Error.Code.Should().Be("DoctorRatings.NotRated");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(6)]
	public async Task Stars_outside_one_to_five_fail_validation(int stars)
	{
		var validator = new UpsertDoctorRatingRequestValidator();

		var result = await validator.ValidateAsync(new UpsertDoctorRatingRequest { Stars = stars });

		result.IsValid.Should().BeFalse();
	}

	[Fact]
	public async Task A_comment_over_the_cap_fails_validation()
	{
		var validator = new UpsertDoctorRatingRequestValidator();

		var result = await validator.ValidateAsync(new UpsertDoctorRatingRequest
		{
			Stars = 4,
			Comment = new string('x', 1001),
		});

		result.IsValid.Should().BeFalse();
		result.Errors.Should().Contain(e => e.PropertyName == nameof(UpsertDoctorRatingRequest.Comment));
	}

	private sealed class FakeClaims : IUserClaimsService
	{
		private readonly string _userId;
		private readonly string _role;

		public FakeClaims(string userId, string role)
		{
			_userId = userId;
			_role = role;
		}

		public string GetUserId() => _userId;
		public string GetUserName() => "Rater Name";
		public string GetRole() => _role;
		public string GetPhotoUrl() => string.Empty;
		public string GetEmail() => "rater@example.com";
	}

	private sealed class FakeHateSpeechDetector : IHateSpeechDetector
	{
		private readonly bool? _isHate;

		public FakeHateSpeechDetector(bool? isHate) => _isHate = isHate;

		public Task<Result<bool>> IsHateSpeech(string content) =>
			_isHate is null
				? Task.FromResult<Result<bool>>(Error.Failure("ML.Unavailable", "down"))
				: Task.FromResult<Result<bool>>(_isHate.Value);
	}
}
