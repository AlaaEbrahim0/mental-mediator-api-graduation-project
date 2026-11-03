using API.Configurations;
using Application.Contracts;
using Application.Dtos.CommentsDtos;
using Application.Dtos.NotificationDtos;
using Application.Dtos.PostsDto;
using Application.Dtos.ReplyDtos;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
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
/// Reactions widened the like ledger and comments/replies gained an attachment.
/// Both changes had to stay additive, so these tests pin the two contracts that
/// matter: the legacy like shape still reads correctly, and an attachment is
/// only honoured when the upload ledger says the commenter owns it.
/// </summary>
public class CommunityReactionTests : IDisposable
{
	private const string DoctorId = "doctor-1";
	private const string PatientId = "patient-1";
	private const string OtherPatientId = "patient-2";

	private readonly AppDbContext _context;
	private readonly IMapper _mapper;
	private readonly FakeStorageService _storage = new();
	private readonly FakeNotificationService _notifications = new();

	public CommunityReactionTests()
	{
		var options = new DbContextOptionsBuilder<AppDbContext>()
			.UseInMemoryDatabase($"community-reactions-{Guid.NewGuid()}")
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

		_context.Clients.Add(new User
		{
			Id = OtherPatientId,
			Email = "other@example.com",
			UserName = "other",
			NormalizedEmail = "OTHER@EXAMPLE.COM",
			NormalizedUserName = "OTHER",
			FirstName = "Ola",
			LastName = "Other",
			Gender = "other",
		});

		_context.SaveChanges();
	}

	public void Dispose() => _context.Dispose();

	[Fact]
	public async Task A_reaction_that_is_not_a_like_leaves_the_like_count_alone()
	{
		var post = AddPost(DoctorId);

		var result = await CreatePostService(PatientId, "Client").SetReaction(post.Id, "love");

		result.IsSuccess.Should().BeTrue();
		result.Value!.MyReaction.Should().Be("love");
		result.Value.LikesCount.Should().Be(0);
		result.Value.IsLikedByCurrentUser.Should().BeFalse();
		result.Value.Reactions.Should().ContainSingle()
			.Which.Reaction.Should().Be("love");
	}

	[Fact]
	public async Task The_like_endpoint_still_writes_and_removes_the_like_reaction()
	{
		var post = AddPost(DoctorId);
		var service = CreatePostService(PatientId, "Client");

		var liked = await service.SetLike(post.Id, true);
		liked.Value!.LikesCount.Should().Be(1);
		liked.Value.IsLikedByCurrentUser.Should().BeTrue();
		liked.Value.MyReaction.Should().Be("like");

		var unliked = await service.SetLike(post.Id, false);
		unliked.Value!.LikesCount.Should().Be(0);
		_context.PostLikes.Should().BeEmpty();
	}

	[Fact]
	public async Task Changing_a_reaction_moves_the_same_row_instead_of_stacking()
	{
		var post = AddPost(DoctorId);
		var service = CreatePostService(PatientId, "Client");

		await service.SetReaction(post.Id, "haha");
		var replaced = await service.SetReaction(post.Id, "wow");

		replaced.Value!.Reactions.Should().ContainSingle()
			.Which.Count.Should().Be(1);
		_context.PostLikes.Should().ContainSingle()
			.Which.Reaction.Should().Be("wow");

		var removed = await service.RemoveReaction(post.Id);
		removed.Value!.MyReaction.Should().BeNull();
		removed.Value.Reactions.Should().BeEmpty();
	}

	[Fact]
	public async Task A_reaction_on_a_post_notifies_its_author_once_per_reactor()
	{
		var post = AddPost(DoctorId);
		var notifications = new FakeNotificationSender(_context);

		var service = CreatePostService(PatientId, "Client", notifications);
		await service.SetReaction(post.Id, "love");
		await service.SetReaction(post.Id, "sad");

		notifications.Sent.Should().ContainSingle();
		notifications.Sent[0].Message.Should().Contain("reacted to");
		notifications.Sent[0].AppUserId.Should().Be(DoctorId);

		var selfNotifications = new FakeNotificationSender();
		await CreatePostService(DoctorId, "Doctor", selfNotifications).SetReaction(post.Id, "wow");
		selfNotifications.Sent.Should().BeEmpty();
	}

	[Fact]
	public async Task A_reaction_outside_the_vocabulary_is_refused()
	{
		var post = AddPost(DoctorId);

		var result = await CreatePostService(PatientId, "Client").SetReaction(post.Id, "angry");

		result.Error!.Code.Should().Be("Reactions.InvalidReaction");
		_context.PostLikes.Should().BeEmpty();
	}

	[Fact]
	public async Task Post_reactions_are_per_reader_in_the_feed()
	{
		var post = AddPost(DoctorId);
		await CreatePostService(PatientId, "Client").SetReaction(post.Id, "like");
		await CreatePostService(OtherPatientId, "Client").SetReaction(post.Id, "love");

		var mine = await CreatePostService(OtherPatientId, "Client")
			.GetPosts(new PostRequestParameters());
		var postView = mine.Value!.Single();
		postView.MyReaction.Should().Be("love");
		postView.IsLikedByCurrentUser.Should().BeFalse();
		postView.LikesCount.Should().Be(1);
		postView.Reactions.Should().HaveCount(2);
	}

	[Fact]
	public async Task A_comment_can_be_only_an_attached_image()
	{
		var post = AddPost(DoctorId);
		var asset = AddAsset("comment-image", PatientId, "image");

		var created = await CreateCommentService(PatientId, "Client")
			.CreateComment(post.Id, new CreateCommentRequest { MediaUrl = asset.Url, MediaType = "image" });

		created.IsSuccess.Should().BeTrue();
		created.Value!.MediaUrl.Should().Be(asset.Url);
		created.Value.MediaType.Should().Be("image");
		created.Value.Content.Should().BeNullOrEmpty();

		var attached = await _context.PostMediaAssets.SingleAsync();
		attached.CommentId.Should().Be(created.Value.Id);
		attached.PostId.Should().BeNull();
	}

	[Fact]
	public async Task A_comment_cannot_attach_a_media_url_the_upload_ledger_does_not_own()
	{
		var post = AddPost(DoctorId);

		var result = await CreateCommentService(PatientId, "Client")
			.CreateComment(post.Id, new CreateCommentRequest
			{
				Content = "Borrowed link",
				MediaUrl = "https://res.cloudinary.com/demo/posts/untracked.jpg",
				MediaType = "image",
			});

		result.Error!.Code.Should().Be("Comments.InvalidMedia");
		_context.Comments.Should().BeEmpty();
	}

	[Fact]
	public async Task A_comment_cannot_attach_someone_elses_upload()
	{
		var post = AddPost(DoctorId);
		var asset = AddAsset("someone-elses", OtherPatientId, "image");

		var result = await CreateCommentService(PatientId, "Client")
			.CreateComment(post.Id, new CreateCommentRequest { MediaUrl = asset.Url, MediaType = "image" });

		result.Error!.Code.Should().Be("Comments.InvalidMedia");
		_context.Comments.Should().BeEmpty();
	}

	[Fact]
	public async Task An_asset_already_attached_to_a_post_cannot_be_reused_by_a_comment()
	{
		var post = AddPost(DoctorId);
		var asset = AddAsset("already-published", PatientId, "image");
		asset.PostId = post.Id;
		_context.SaveChanges();

		var result = await CreateCommentService(PatientId, "Client")
			.CreateComment(post.Id, new CreateCommentRequest { MediaUrl = asset.Url, MediaType = "image" });

		result.Error!.Code.Should().Be("Comments.InvalidMedia");
	}

	[Fact]
	public async Task A_reply_can_be_only_a_video_and_is_tracked_too()
	{
		var post = AddPost(DoctorId);
		var comment = AddComment(post.Id, PatientId, "How did you cope?");
		var asset = AddAsset("reply-clip", DoctorId, "video");

		var created = await CreateReplyService(DoctorId, "Doctor")
			.CreateReply(post.Id, comment.Id, new CreateReplyRequest { MediaUrl = asset.Url, MediaType = "video" });

		created.IsSuccess.Should().BeTrue();
		created.Value!.MediaType.Should().Be("video");
		_context.PostMediaAssets.Single(item => item.Url == asset.Url).ReplyId
			.Should().Be(created.Value.Id);
	}

	[Fact]
	public async Task Deleting_a_comment_destroys_its_attachment_and_its_replies_attachments()
	{
		var post = AddPost(DoctorId);
		var commenter = CreateCommentService(PatientId, "Client");

		var comment = await commenter.CreateComment(post.Id, new CreateCommentRequest
		{
			Content = "Has a picture",
			MediaUrl = AddAsset("comment-x", PatientId, "image").Url,
			MediaType = "image",
		});
		await CreateReplyService(DoctorId, "Doctor").CreateReply(post.Id, comment.Value!.Id, new CreateReplyRequest
		{
			Content = "And a clip",
			MediaUrl = AddAsset("reply-x", DoctorId, "video").Url,
			MediaType = "video",
		});

		_context.PostMediaAssets.Should().HaveCount(2);

		var deleted = await commenter.DeleteComment(post.Id, comment.Value.Id);

		deleted.IsSuccess.Should().BeTrue();
		_context.PostMediaAssets.Should().BeEmpty();
		_storage.DeletedPublicIds.Should().BeEquivalentTo(new[] { "comment-x", "reply-x" });
	}

	[Fact]
	public async Task A_comment_reaction_is_stored_replaced_and_removed()
	{
		var post = AddPost(DoctorId);
		var comment = AddComment(post.Id, PatientId, "Readable comment");
		var service = CreateCommentService(DoctorId, "Doctor");

		var reacted = await service.SetReaction(post.Id, comment.Id, "love");
		reacted.IsSuccess.Should().BeTrue();
		reacted.Value!.MyReaction.Should().Be("love");
		reacted.Value.Reactions.Should().ContainSingle().Which.Count.Should().Be(1);
		_context.CommentReactions.Should().ContainSingle().Which.Reaction.Should().Be("love");

		var replaced = await service.SetReaction(post.Id, comment.Id, "haha");
		replaced.Value!.MyReaction.Should().Be("haha");
		_context.CommentReactions.Should().ContainSingle().Which.Reaction.Should().Be("haha");

		var removed = await service.RemoveReaction(post.Id, comment.Id);
		removed.Value!.Reactions.Should().BeEmpty();
		_context.CommentReactions.Should().BeEmpty();
	}

	[Fact]
	public async Task Comment_reactions_are_listed_per_reader()
	{
		var post = AddPost(DoctorId);
		var comment = AddComment(post.Id, PatientId, "Two readers");
		await CreateCommentService(DoctorId, "Doctor").SetReaction(post.Id, comment.Id, "like");
		await CreateCommentService(OtherPatientId, "Client").SetReaction(post.Id, comment.Id, "sad");

		var listed = await CreateCommentService(OtherPatientId, "Client").GetCommentsByPostId(post.Id);

		listed.Value.Should().ContainSingle();
		listed.Value!.Single().MyReaction.Should().Be("sad");
		listed.Value.Single().Reactions.Should().HaveCount(2);
	}

	[Fact]
	public async Task A_reply_reaction_is_stored_and_removed()
	{
		var post = AddPost(DoctorId);
		var comment = AddComment(post.Id, PatientId, "Parent comment");
		var reply = AddReply(comment.Id, DoctorId, "A reply");
		var service = CreateReplyService(PatientId, "Client");

		var reacted = await service.SetReaction(post.Id, comment.Id, reply.Id, "wow");
		reacted.IsSuccess.Should().BeTrue();
		reacted.Value!.MyReaction.Should().Be("wow");
		_context.ReplyReactions.Should().ContainSingle().Which.Reaction.Should().Be("wow");

		var removed = await service.RemoveReaction(post.Id, comment.Id, reply.Id);
		removed.Value!.Reactions.Should().BeEmpty();
		_context.ReplyReactions.Should().BeEmpty();
	}

	[Fact]
	public async Task An_unknown_reaction_token_never_reaches_the_database()
	{
		var post = AddPost(DoctorId);
		var comment = AddComment(post.Id, PatientId, "No angry button");

		var onComment = await CreateCommentService(DoctorId, "Doctor")
			.SetReaction(post.Id, comment.Id, "angry");
		var onPost = await CreatePostService(DoctorId, "Doctor").SetReaction(post.Id, "angry");

		onComment.Error!.Code.Should().Be("Reactions.InvalidReaction");
		onPost.Error!.Code.Should().Be("Reactions.InvalidReaction");
		_context.CommentReactions.Should().BeEmpty();
		_context.PostLikes.Should().BeEmpty();
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

	private Comment AddComment(int postId, string authorId, string content)
	{
		var comment = new Comment
		{
			PostId = postId,
			AppUserId = authorId,
			Username = "Pat Patient",
			Content = content,
			CommentedAt = DateTime.UtcNow,
		};

		_context.Comments.Add(comment);
		_context.SaveChanges();
		return comment;
	}

	private Reply AddReply(int commentId, string authorId, string content)
	{
		var reply = new Reply
		{
			CommentId = commentId,
			AppUserId = authorId,
			Username = "Dana Doctor",
			Content = content,
			RepliedAt = DateTime.UtcNow,
		};

		_context.Replies.Add(reply);
		_context.SaveChanges();
		return reply;
	}

	private PostMediaAsset AddAsset(string publicId, string ownerId, string mediaType)
	{
		var asset = new PostMediaAsset
		{
			Url = $"https://res.cloudinary.com/demo/posts/{publicId}.{(mediaType == "video" ? "mp4" : "jpg")}",
			PublicId = publicId,
			MediaType = mediaType,
			OwnerId = ownerId,
			CreatedAtUtc = DateTime.UtcNow,
		};

		_context.PostMediaAssets.Add(asset);
		_context.SaveChanges();
		return asset;
	}

	private PostService CreatePostService(
		string userId,
		string role,
		FakeNotificationSender? notificationSender = null) =>
		new(
			new RepositoryManager(_context),
			_mapper,
			new FakeClaims(userId, role),
			new FakeHateSpeechDetector(),
			new FakeCacheService(),
			_storage,
			notificationSender ?? new FakeNotificationSender(_context));

	private CommentService CreateCommentService(string userId, string role) =>
		new(
			new RepositoryManager(_context),
			_mapper,
			new FakeClaims(userId, role),
			null!,
			_notifications,
			new FakeHateSpeechDetector(),
			new FakeCacheService(),
			logger: null,
			storageService: _storage);

	private ReplyService CreateReplyService(string userId, string role) =>
		new(
			new RepositoryManager(_context),
			_mapper,
			new FakeClaims(userId, role),
			_notifications,
			new FakeHateSpeechDetector(),
			new FakeCacheService(),
			logger: null,
			storageService: _storage);

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
		public string GetUserName() => "Test Member";
		public string GetRole() => _role;
		public string GetPhotoUrl() => string.Empty;
		public string GetEmail() => "member@example.com";
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
		public List<string> DeletedPublicIds { get; } = new();

		public Task<Result<string>> UploadPhoto(IFormFile photo) =>
			Task.FromResult<Result<string>>("https://res.cloudinary.com/demo/posts/photo.jpg");

		public Task<Result<StoredMedia>> UploadMedia(IFormFile file) =>
			Task.FromResult<Result<StoredMedia>>(new StoredMedia(
				"https://res.cloudinary.com/demo/posts/file.jpg",
				"image",
				"posts/file"));

		public bool IsManagedMediaUrl(string url) =>
			url.StartsWith("https://res.cloudinary.com/", StringComparison.Ordinal);

		public Task<Result> DeleteMediaAsync(string publicId, string mediaType)
		{
			DeletedPublicIds.Add(publicId);
			return Task.FromResult(Result.Success());
		}
	}

	/// <summary>
	/// Comment and reply notifications go through the read/write notification
	/// service; only the write half is on these paths.
	/// </summary>
	private sealed class FakeNotificationService : INotificationService
	{
		public List<Notification> Sent { get; } = new();

		public Task SendNotificationAsync(Notification notification)
		{
			Sent.Add(notification);
			return Task.CompletedTask;
		}

		public Task<Result<IEnumerable<NotificationResponse>>> GetNotificationByUserId(
			string userId, RequestParameters request) =>
			throw new NotSupportedException();

		public Task<Result<NotificationResponse>> GetNotificationById(int id) =>
			throw new NotSupportedException();

		public Task<Result<IEnumerable<NotificationResponse>>> GetCurrentUserNotifications(
			RequestParameters request) =>
			throw new NotSupportedException();

		public Task<Result<bool>> MarkAllAsReadAsync() => throw new NotSupportedException();
		public Task<Result<bool>> MarkAsReadAsync(int id) => throw new NotSupportedException();
		public Task<Result<int>> GetUnreadCountAsync() => throw new NotSupportedException();
		public Task<Result<bool>> MarkMultipleAsReadAsync(IEnumerable<int> ids) =>
			throw new NotSupportedException();
	}
}
