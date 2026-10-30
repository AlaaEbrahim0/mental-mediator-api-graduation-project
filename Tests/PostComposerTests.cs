using API.Configurations;
using Application.Community;
using Application.Contracts;
using Application.Dtos.PostsDto;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using FluentAssertions;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shared;
using Shared.RequestParameters;
using Xunit;

namespace MentalMediator.Tests;

/// <summary>
/// The composer adds three things with privacy consequences: audiences,
/// anonymity and uploaded media. These tests run the production repositories
/// over an in-memory store so the feed filters and the response redaction are
/// exercised end to end rather than through mocks.
/// </summary>
public class PostComposerTests : IDisposable
{
	private const string DoctorId = "doctor-1";
	private const string PatientId = "patient-1";

	private readonly AppDbContext _context;
	private readonly IMapper _mapper;
	private readonly FakeStorageService _storage = new();

	public PostComposerTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"posts-{Guid.NewGuid()}")
			.Options;

		_context = new AppDbContext(options);
		_mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();

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
			Specialization = DoctorSpecialization.ClinicalPsychology,
		});

		_context.Clients.Add(new User
		{
			Id = PatientId,
			Email = "patient@example.com",
			UserName = "patient",
			NormalizedEmail = "PATIENT@EXAMPLE.COM",
			NormalizedUserName = "PATIENT",
			FirstName = "Pat",
			LastName = "Patient",
			Gender = "other",
		});

		_context.SaveChanges();
	}

	public void Dispose() => _context.Dispose();

	[Fact]
	public async Task Patient_feed_hides_doctors_only_posts()
	{
		AddPost(DoctorId, PostVisibility.Public);
		AddPost(DoctorId, PostVisibility.DoctorsOnly);

		var result = await CreateService(PatientId, "Client")
			.GetPosts(new PostRequestParameters());

		result.IsSuccess.Should().BeTrue();
		result.Value.Should().ContainSingle()
			.Which.Visibility.Should().Be(PostVisibility.Public);
	}

	[Fact]
	public async Task A_forged_include_flag_cannot_unlock_the_doctors_only_audience()
	{
		AddPost(DoctorId, PostVisibility.DoctorsOnly);

		var result = await CreateService(DoctorId, "Doctor")
			.GetPosts(new PostRequestParameters { IncludeDoctorsOnly = false });

		result.Value.Should().ContainSingle();
		result = await CreateService(PatientId, "Client")
			.GetPosts(new PostRequestParameters { IncludeDoctorsOnly = true });
		result.Value.Should().BeEmpty();
	}

	[Fact]
	public async Task Patient_cannot_read_like_or_open_comments_on_a_doctors_only_post()
	{
		var post = AddPost(DoctorId, PostVisibility.DoctorsOnly);
		var service = CreateService(PatientId, "Client");

		(await service.GetPostById(post.Id)).Error.Should().Be(PostErrors.NotFound(post.Id));
		(await service.SetLike(post.Id, true)).Error.Should().Be(PostErrors.NotFound(post.Id));

		var comments = new CommentService(
			new RepositoryManager(_context),
			_mapper,
			new FakeClaims(PatientId, "Client"),
			null!,
			null!,
			new FakeHateSpeechDetector(),
			new FakeCacheService());

		(await comments.GetCommentsByPostId(post.Id)).Error.Should().Be(PostErrors.NotFound(post.Id));
	}

	[Fact]
	public async Task Anonymous_posts_redact_identity_and_flag_the_authors_own_post()
	{
		var post = AddPost(DoctorId, isAnonymous: true);

		var authorView = await CreateService(DoctorId, "Doctor").GetPostById(post.Id);
		authorView.IsSuccess.Should().BeTrue();
		authorView.Value!.AppUserId.Should().BeNull();
		authorView.Value.Username.Should().BeNull();
		authorView.Value.IsOwnPost.Should().BeTrue();

		var otherView = await CreateService(PatientId, "Client").GetPostById(post.Id);
		otherView.Value!.IsOwnPost.Should().BeFalse();
		otherView.Value.AppUserId.Should().BeNull();
	}

	[Fact]
	public async Task Confessions_feed_lists_anonymous_posts_without_identity()
	{
		AddPost(DoctorId, isAnonymous: true);

		var result = await CreateService(PatientId, "Client")
			.GetPosts(new PostRequestParameters { ConfessionsOnly = true });

		result.Value.Should().ContainSingle();
		result.Value.Single().Username.Should().BeNull();
		result.Value.Single().AppUserId.Should().BeNull();
	}

	[Fact]
	public async Task A_patient_cannot_publish_to_the_doctors_only_audience()
	{
		var result = await CreateService(PatientId, "Client")
			.CreatePostAsync(NewRequest(PostVisibility.DoctorsOnly));

		result.Error.Code.Should().Be("Posts.DoctorsOnlyForbidden");
		_context.Posts.Should().BeEmpty();
	}

	[Fact]
	public async Task A_doctor_can_publish_to_the_doctors_only_audience()
	{
		var result = await CreateService(DoctorId, "Doctor")
			.CreatePostAsync(NewRequest(PostVisibility.DoctorsOnly));

		result.IsSuccess.Should().BeTrue();
		result.Value.Visibility.Should().Be(PostVisibility.DoctorsOnly);
	}

	[Fact]
	public async Task The_specific_group_audience_is_rejected_until_groups_exist()
	{
		var result = await CreateService(DoctorId, "Doctor")
			.CreatePostAsync(NewRequest(PostVisibility.SpecificGroup));

		result.Error.Code.Should().Be("Posts.VisibilityUnsupported");
	}

	[Fact]
	public async Task Title_is_optional_and_taxonomy_is_normalized()
	{
		var request = NewRequest();
		request.Title = "   ";
		request.Category = "clinical case";
		request.Tags = "depression, Depression,Anxiety";

		var result = await CreateService(DoctorId, "Doctor").CreatePostAsync(request);

		result.IsSuccess.Should().BeTrue();
		result.Value.Title.Should().BeEmpty();
		result.Value.Category.Should().Be("Clinical Case");
		result.Value.Tags.Should().Be("Depression,Anxiety");
		result.Value.IsOwnPost.Should().BeTrue();
	}

	[Fact]
	public async Task Media_outside_the_managed_cloud_is_rejected()
	{
		_storage.AcceptManagedUrls = false;
		var request = NewRequest();
		request.Media = "[{\"url\":\"https://evil.example/x.jpg\",\"type\":\"image\"}]";

		var result = await CreateService(DoctorId, "Doctor").CreatePostAsync(request);

		result.Error.Code.Should().Be("Posts.InvalidMedia");
		_context.Posts.Should().BeEmpty();
	}

	[Fact]
	public async Task Uploaded_media_is_persisted_and_the_first_image_is_the_legacy_photo()
	{
		var video = "https://res.cloudinary.com/demo/video/upload/posts/a.mp4";
		var image = "https://res.cloudinary.com/demo/image/upload/posts/b.jpg";
		var request = NewRequest();
		request.Media = PostMediaCodec.Serialize(new[]
		{
			new PostMediaRequest { Url = video, Type = "video" },
			new PostMediaRequest { Url = image, Type = "image" },
		});

		var result = await CreateService(DoctorId, "Doctor").CreatePostAsync(request);

		result.IsSuccess.Should().BeTrue();
		result.Value.Media.Should().HaveCount(2);
		result.Value.PostPhotoUrl.Should().Be(image);

		var saved = await _context.Posts.SingleAsync();
		PostMediaCodec.ToRequestList(saved.MediaJson).Should().HaveCount(2);
	}

	[Fact]
	public void The_validator_accepts_a_composer_payload_without_a_title()
	{
		var validation = new CreatePostRequestValidator().Validate(new CreatePostRequest
		{
			Content = "A short update.",
			Category = "General Discussion",
			Tags = "Anxiety,Support",
			Media = PostMediaCodec.Serialize(new[]
			{
				new PostMediaRequest { Url = "https://res.cloudinary.com/demo/image/upload/posts/a.jpg", Type = "image" },
			}),
		});

		validation.IsValid.Should().BeTrue();
	}

	[Theory]
	[InlineData("Update", "Free text category", null, null)]
	[InlineData("Update", "General Discussion", "NotARealTag", null)]
	[InlineData("Update", "General Discussion", null, "[{\"url\":\"http://res.cloudinary.com/demo/a.jpg\",\"type\":\"image\"}]")]
	public void The_validator_rejects_payloads_outside_the_taxonomy(
		string? title,
		string category,
		string? tags,
		string? media)
	{
		var validation = new CreatePostRequestValidator().Validate(new CreatePostRequest
		{
			Title = title,
			Content = "Body",
			Category = category,
			Tags = tags,
			Media = media,
		});

		validation.IsValid.Should().BeFalse();
	}

	[Fact]
	public void The_validator_rejects_the_specific_group_audience()
	{
		var validation = new CreatePostRequestValidator().Validate(new CreatePostRequest
		{
			Content = "Body",
			Category = "General Discussion",
			Visibility = PostVisibility.SpecificGroup,
		});

		validation.IsValid.Should().BeFalse();
		validation.Errors.Should().Contain(error => error.ErrorMessage.Contains("specific group"));
	}

	[Fact]
	public void Media_codec_normalizes_types_and_enforces_the_caps()
	{
		PostMediaCodec.TryParse(
			"[{\"url\":\"https://res.cloudinary.com/demo/a.jpg\",\"type\":\" IMAGE \"}]",
			out var items,
			out var error).Should().BeTrue();
		error.Should().BeNull();
		items.Single().Type.Should().Be("image");

		PostMediaCodec.TryParse(
			"[{\"url\":\"https://res.cloudinary.com/demo/a.jpg\",\"type\":\"image\"}," +
			"{\"url\":\"https://res.cloudinary.com/demo/a.jpg\",\"type\":\"image\"}]",
			out _,
			out error).Should().BeFalse();
		error.Should().Contain("more than once");

		var fiveImages = Enumerable.Range(0, 5)
			.Select(index => new PostMediaRequest
			{
				Url = $"https://res.cloudinary.com/demo/{index}.jpg",
				Type = "image",
			});
		PostMediaCodec.TryParse(PostMediaCodec.Serialize(fiveImages), out _, out error).Should().BeFalse();
		error.Should().Contain("at most 4 images");
	}

	[Fact]
	public void Tag_normalization_caps_the_selection_and_requires_standard_tags()
	{
		PostTaxonomy.TryNormalizeTags(
			new[] { "Anxiety", "anxiety", "Depression" },
			out var csv,
			out var error).Should().BeTrue();
		error.Should().BeNull();
		csv.Should().Be("Anxiety,Depression");

		PostTaxonomy.TryNormalizeTags(new[] { "Anxiety", "NotATag" }, out _, out error)
			.Should().BeFalse();
		error.Should().Contain("standard tags");

		PostTaxonomy.TryNormalizeTags(
			new[] { "Anxiety", "Depression", "Stress", "Sleep", "Relationships", "Family" },
			out _,
			out error).Should().BeFalse();
		error.Should().Contain("at most 5 tags");
	}

	private PostService CreateService(string userId, string role) =>
		new(
			new RepositoryManager(_context),
			_mapper,
			new FakeClaims(userId, role),
			new FakeHateSpeechDetector(),
			new FakeCacheService(),
			_storage);

	private static CreatePostRequest NewRequest(PostVisibility visibility = PostVisibility.Public) =>
		new()
		{
			Title = "A title",
			Content = "Post body",
			Category = "General Discussion",
			Visibility = visibility,
		};

	private Post AddPost(
		string authorId,
		PostVisibility visibility = PostVisibility.Public,
		bool isAnonymous = false)
	{
		var post = new Post
		{
			AppUserId = authorId,
			Title = "Title",
			Content = "Content",
			Category = "General Discussion",
			PostedOn = DateTime.UtcNow,
			Visibility = visibility,
			IsAnonymous = isAnonymous,
		};

		_context.Posts.Add(post);
		_context.SaveChanges();
		return post;
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
		public string GetUserName() => "Dana Doctor";
		public string GetRole() => _role;
		public string GetPhotoUrl() => string.Empty;
		public string GetEmail() => "doctor@example.com";
	}

	private sealed class FakeHateSpeechDetector : IHateSpeechDetector
	{
		public Task<Result<bool>> IsHateSpeech(string content) => Task.FromResult<Result<bool>>(false);
	}

	private sealed class FakeCacheService : ICacheService
	{
		public Task<T?> GetAsync<T>(string key) where T : class => Task.FromResult<T?>(null);
		public Task SetAsync<T>(string key, T value, TimeSpan expirationTime) => Task.CompletedTask;
		public Task RemoveAsync(string key) => Task.CompletedTask;
		public Task RemoveByAsync(string prefix) => Task.CompletedTask;
	}

	private sealed class FakeStorageService : IStorageService
	{
		public bool AcceptManagedUrls { get; set; } = true;
		public Result<string> PhotoResult { get; set; } = "https://res.cloudinary.com/demo/posts/photo.jpg";

		public Task<Result<string>> UploadPhoto(IFormFile photo) => Task.FromResult(PhotoResult);

		public Task<Result<StoredMedia>> UploadMedia(IFormFile file) =>
			Task.FromResult<Result<StoredMedia>>(new StoredMedia(
				"https://res.cloudinary.com/demo/posts/file.jpg", "image"));

		public bool IsManagedMediaUrl(string url) =>
			AcceptManagedUrls && url.StartsWith("https://res.cloudinary.com/", StringComparison.Ordinal);
	}
}
