using API.Configurations;
using Application.Community;
using Application.Contracts;
using Application.Dtos.CommentsDtos;
using Application.Dtos.PostsDto;
using Application.Dtos.ReplyDtos;
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
	public async Task The_specific_group_audience_requires_a_target_group()
	{
		var result = await CreateService(DoctorId, "Doctor")
			.CreatePostAsync(NewRequest(PostVisibility.SpecificGroup));

		result.Error.Code.Should().Be("Posts.SpecificGroupRequired");
	}

	[Fact]
	public async Task A_group_post_is_readable_only_by_active_members()
	{
		var room = AddRoom(DoctorId);
		var request = NewRequest(PostVisibility.SpecificGroup);
		request.TargetGroupId = room.Id;

		var created = await CreateService(DoctorId, "Doctor").CreatePostAsync(request);

		created.IsSuccess.Should().BeTrue();
		created.Value.TargetGroupId.Should().Be(room.Id);

		var memberFeed = await CreateService(DoctorId, "Doctor")
			.GetPosts(new PostRequestParameters());
		memberFeed.Value.Should().ContainSingle();

		var patientFeed = await CreateService(PatientId, "Client")
			.GetPosts(new PostRequestParameters());
		patientFeed.Value.Should().BeEmpty();

		var patientRead = await CreateService(PatientId, "Client")
			.GetPostById(created.Value.Id);
		patientRead.Error.Should().Be(PostErrors.NotFound(created.Value.Id));
	}

	[Fact]
	public async Task A_user_cannot_publish_to_a_group_they_do_not_belong_to()
	{
		var room = AddRoom(DoctorId);
		var request = NewRequest(PostVisibility.SpecificGroup);
		request.TargetGroupId = room.Id;

		var result = await CreateService(PatientId, "Client").CreatePostAsync(request);

		result.Error.Code.Should().Be("Posts.SpecificGroupForbidden");
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
	[InlineData("Update", "General Discussion", null, "[{\"url\":\"http://insecure.example/a.jpg\",\"type\":\"image\"}]")]
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
	public void The_validator_requires_a_target_group_for_the_specific_group_audience()
	{
		var validation = new CreatePostRequestValidator().Validate(new CreatePostRequest
		{
			Content = "Body",
			Category = "General Discussion",
			Visibility = PostVisibility.SpecificGroup,
		});

		validation.IsValid.Should().BeFalse();
		validation.Errors.Should().Contain(error => error.ErrorMessage.Contains("Choose a group"));
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
	public async Task Drafts_are_owner_only_and_excluded_from_the_public_feed()
	{
		var request = new CreatePostRequest
		{
			Title = "Work in progress",
			Content = "",
			IsDraft = true,
			Visibility = PostVisibility.Public,
		};

		var created = await CreateService(PatientId, "Client").CreatePostAsync(request);

		created.IsSuccess.Should().BeTrue();
		created.Value.IsDraft.Should().BeTrue();

		var feed = await CreateService(PatientId, "Client").GetPosts(new PostRequestParameters());
		feed.Value.Should().BeEmpty();

		var latest = await CreateService(PatientId, "Client").GetLatestDraft();
		latest.Value.Should().NotBeNull();
		latest.Value!.Id.Should().Be(created.Value.Id);

		var otherRead = await CreateService(DoctorId, "Doctor").GetPostById(created.Value.Id);
		otherRead.Error.Should().Be(PostErrors.NotFound(created.Value.Id));
	}

	[Fact]
	public async Task Publishing_a_draft_requires_content_and_a_category()
	{
		var draft = await CreateService(PatientId, "Client").CreatePostAsync(new CreatePostRequest
		{
			Title = "Draft",
			Content = "",
			IsDraft = true,
			Visibility = PostVisibility.Public,
		});

		var incomplete = await CreateService(PatientId, "Client").UpdatePost(
			draft.Value.Id,
			new UpdatePostRequest { Title = "Draft", Content = "", Category = null, IsDraft = false });

		incomplete.Error.Code.Should().Be("Posts.ContentRequired");

		var published = await CreateService(PatientId, "Client").UpdatePost(
			draft.Value.Id,
			new UpdatePostRequest
			{
				Title = "Draft",
				Content = "Now ready",
				Category = "General Discussion",
				IsDraft = false,
			});

		published.IsSuccess.Should().BeTrue();
		published.Value.IsDraft.Should().BeFalse();

		var feed = await CreateService(PatientId, "Client").GetPosts(new PostRequestParameters());
		feed.Value.Should().ContainSingle(post => post.Id == draft.Value.Id);

		var latest = await CreateService(PatientId, "Client").GetLatestDraft();
		latest.Value.Should().BeNull();
	}

	[Fact]
	public async Task Restricted_post_content_cannot_be_read_through_comment_or_reply_endpoints()
	{
		var post = AddPost(DoctorId, PostVisibility.DoctorsOnly);
		var comment = new Comment
		{
			PostId = post.Id,
			AppUserId = PatientId,
			Content = "Restricted discussion",
			CommentedAt = DateTime.UtcNow,
		};
		_context.Comments.Add(comment);
		_context.SaveChanges();

		var reply = new Reply
		{
			CommentId = comment.Id,
			AppUserId = PatientId,
			Content = "Restricted reply",
			RepliedAt = DateTime.UtcNow,
		};
		_context.Replies.Add(reply);
		_context.SaveChanges();

		var comments = new CommentService(
			new RepositoryManager(_context),
			_mapper,
			new FakeClaims(PatientId, "Client"),
			null!,
			null!,
			new FakeHateSpeechDetector(),
			new FakeCacheService());

		(await comments.GetCommentById(post.Id, comment.Id))
			.Error.Should().Be(CommentErrors.NotFound(comment.Id));

		var replies = new ReplyService(
			new RepositoryManager(_context),
			_mapper,
			new FakeClaims(PatientId, "Client"),
			null!,
			new FakeHateSpeechDetector(),
			new FakeCacheService());

		(await replies.GetRepliesForComment(post.Id, comment.Id))
			.Error.Should().Be(CommentErrors.NotFound(comment.Id));
		(await replies.GetReplyById(post.Id, comment.Id, reply.Id))
			.Error.Should().Be(ReplyErrors.NotFound(reply.Id));
	}

	[Fact]
	public async Task Uploaded_media_is_tracked_attached_and_removed_with_the_post()
	{
		var service = CreateService(DoctorId, "Doctor");
		var file = new FormFile(new MemoryStream(new byte[] { 1, 2, 3 }), 0, 3, "file", "a.jpg")
		{
			Headers = new HeaderDictionary(),
			ContentType = "image/jpeg",
		};

		var upload = await service.UploadMediaAsync(file);
		upload.IsSuccess.Should().BeTrue();

		var asset = await _context.PostMediaAssets.SingleAsync();
		asset.PostId.Should().BeNull();

		var request = NewRequest();
		request.Media = PostMediaCodec.Serialize(new[]
		{
			new PostMediaRequest { Url = upload.Value.Url, Type = "image" },
		});

		var created = await service.CreatePostAsync(request);
		created.IsSuccess.Should().BeTrue();

		asset = await _context.PostMediaAssets.SingleAsync();
		asset.PostId.Should().Be(created.Value.Id);

		var deleted = await service.DeletePost(created.Value.Id);
		deleted.IsSuccess.Should().BeTrue();

		_context.PostMediaAssets.Should().BeEmpty();
		_storage.DeletedPublicIds.Should().Contain("posts/file");
	}

	[Fact]
	public async Task Anonymous_authors_keep_internal_ownership_while_responses_are_redacted()
	{
		var post = AddPost(DoctorId, isAnonymous: true);

		var commenter = new CommentService(
			new RepositoryManager(_context),
			_mapper,
			new FakeClaims(DoctorId, "Doctor"),
			null!,
			null!,
			new FakeHateSpeechDetector(),
			new FakeCacheService());

		var createdComment = await commenter.CreateComment(post.Id, new CreateCommentRequest
		{
			Content = "A private author's comment",
		});

		createdComment.IsSuccess.Should().BeTrue();
		createdComment.Value.AppUserId.Should().BeNull();
		createdComment.Value.Username.Should().BeNull();

		var storedComment = await _context.Comments.SingleAsync();
		storedComment.AppUserId.Should().Be(DoctorId);

		var replies = new ReplyService(
			new RepositoryManager(_context),
			_mapper,
			new FakeClaims(DoctorId, "Doctor"),
			null!,
			new FakeHateSpeechDetector(),
			new FakeCacheService());

		var createdReply = await replies.CreateReply(post.Id, storedComment.Id, new CreateReplyRequest
		{
			Content = "A private author's reply",
		});

		createdReply.IsSuccess.Should().BeTrue();
		createdReply.Value.AppUserId.Should().BeNull();
		createdReply.Value.Username.Should().BeNull();

		var storedReply = await _context.Replies.SingleAsync();
		storedReply.AppUserId.Should().Be(DoctorId);

		var reader = new ReplyService(
			new RepositoryManager(_context),
			_mapper,
			new FakeClaims(PatientId, "Client"),
			null!,
			new FakeHateSpeechDetector(),
			new FakeCacheService());

		var list = await reader.GetRepliesForComment(post.Id, storedComment.Id);
		list.Value.Should().ContainSingle();
		list.Value!.Single().AppUserId.Should().BeNull();
		list.Value!.Single().Username.Should().BeNull();

		var direct = await reader.GetReplyById(post.Id, storedComment.Id, storedReply.Id);
		direct.Value!.AppUserId.Should().BeNull();
		direct.Value!.Username.Should().BeNull();
	}

	[Fact]
	public void Cloudinary_http_urls_are_normalized_to_https_at_the_response_boundary()
	{
		var response = PostMediaCodec.ToResponse(
			"[{\"url\":\"http://res.cloudinary.com/demo/image/upload/a.jpg\",\"type\":\"image\"}]");

		response.Single().Url.Should().Be(
			"https://res.cloudinary.com/demo/image/upload/a.jpg");
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

	private ChatRoom AddRoom(string ownerId)
	{
		var room = new ChatRoom
		{
			Name = "Support room",
			Description = "A room for the group audience test.",
			CreatedById = ownerId,
			CreatedAtUtc = DateTime.UtcNow,
		};
		_context.ChatRooms.Add(room);
		_context.SaveChanges();

		_context.ChatRoomMembers.Add(new ChatRoomMember
		{
			RoomId = room.Id,
			UserId = ownerId,
			DisplayName = "Owner",
			IsDoctor = true,
			JoinedAtUtc = DateTime.UtcNow,
		});
		_context.SaveChanges();
		return room;
	}

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
				"https://res.cloudinary.com/demo/posts/file.jpg",
				"image",
				"posts/file"));

		public bool IsManagedMediaUrl(string url) =>
			AcceptManagedUrls && url.StartsWith("https://res.cloudinary.com/", StringComparison.Ordinal);

		public List<string> DeletedPublicIds { get; } = new();

		public Task<Result> DeleteMediaAsync(string publicId, string mediaType)
		{
			DeletedPublicIds.Add(publicId);
			return Task.FromResult(Result.Success());
		}
	}
}
