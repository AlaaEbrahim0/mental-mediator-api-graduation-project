using Application.Contracts;
using Application.Services;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// Profile visibility is a server-side privacy boundary. A private patient is
/// invisible to strangers; a public patient still never leaks contact details
/// to anonymous callers. Only the owner and an admin see the full record.
/// </summary>
public class ProfileVisibilityTests : IDisposable
{
	private const string PatientId = "patient-1";
	private const string StrangerId = "stranger-1";
	private const string AdminId = "admin-1";

	private readonly AppDbContext _context;

	public ProfileVisibilityTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"visibility-{Guid.NewGuid()}")
			.Options;
		_context = new AppDbContext(options);

		_context.Clients.Add(new User
		{
			Id = PatientId,
			Email = "patient@example.com",
			UserName = "patient",
			NormalizedEmail = "PATIENT@EXAMPLE.COM",
			NormalizedUserName = "PATIENT",
			FirstName = "Pat",
			LastName = "Patient",
			Gender = "female",
			City = "Cairo",
			ContactEmail = "secret@example.com",
			ContactPhone = "+200000000000",
			ContactAddress = "Private home address",
			IsProfilePublic = false,
		});
		_context.SaveChanges();
	}

	public void Dispose() => _context.Dispose();

	[Fact]
	public async Task A_private_profile_is_invisible_to_a_stranger()
	{
		var service = CreateService(StrangerId, "User");
		var result = await service.GetPublicProfile(PatientId);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Profiles.NotFound");
	}

	[Fact]
	public async Task A_private_profile_is_invisible_to_anonymous_callers()
	{
		var service = CreateService(null, null);
		var result = await service.GetPublicProfile(PatientId);

		result.IsFailure.Should().BeTrue();
		result.Error.Code.Should().Be("Profiles.NotFound");
	}

	[Fact]
	public async Task The_owner_can_always_open_their_own_private_profile()
	{
		var service = CreateService(PatientId, "User");
		var result = await service.GetPublicProfile(PatientId);

		result.IsSuccess.Should().BeTrue();
		result.Value.Contact.Email.Should().Be("secret@example.com");
	}

	[Fact]
	public async Task A_public_profile_hides_contact_details_from_strangers()
	{
		SetPublic(true);
		var service = CreateService(null, null);

		var result = await service.GetPublicProfile(PatientId);

		result.IsSuccess.Should().BeTrue();
		result.Value.FullName.Should().Be("Pat Patient");
		result.Value.City.Should().Be("Cairo");
		result.Value.Contact.Email.Should().BeNullOrEmpty();
		result.Value.Contact.Phone.Should().BeNullOrEmpty();
		result.Value.Contact.Address.Should().BeNullOrEmpty();
	}

	[Fact]
	public async Task An_admin_sees_contact_details_on_a_public_profile()
	{
		SetPublic(true);
		var service = CreateService(AdminId, "Admin");

		var result = await service.GetPublicProfile(PatientId);

		result.IsSuccess.Should().BeTrue();
		result.Value.Contact.Email.Should().Be("secret@example.com");
	}

	private void SetPublic(bool value)
	{
		var user = _context.Clients.First(item => item.Id == PatientId);
		user.IsProfilePublic = value;
		_context.SaveChanges();
	}

	private ProfileService CreateService(string? userId, string? role) =>
		new(
			new RepositoryManager(_context),
			new FakeClaims(userId, role),
			new FakeStorage());

	private sealed class FakeClaims : IUserClaimsService
	{
		private readonly string? _userId;
		private readonly string? _role;
		public FakeClaims(string? userId, string? role) { _userId = userId; _role = role; }
		public string GetUserId() => _userId ?? string.Empty;
		public string GetUserName() => "Test";
		public string GetRole() => _role ?? string.Empty;
		public string GetPhotoUrl() => string.Empty;
		public string GetEmail() => "test@example.com";
	}

	private sealed class FakeStorage : IStorageService
	{
		public Task<Result<string>> UploadPhoto(IFormFile photo) => Task.FromResult(Result<string>.Success(string.Empty));
		public Task<Result<StoredMedia>> UploadMedia(IFormFile file) => Task.FromResult(Result<StoredMedia>.Success(new StoredMedia(string.Empty, "image")));
		public bool IsManagedMediaUrl(string url) => false;
		public Task<Result> DeleteMediaAsync(string publicId, string mediaType) => Task.FromResult(Result.Success());
	}
}
