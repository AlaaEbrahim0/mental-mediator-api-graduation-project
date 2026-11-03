using Application.Community;
using Application.Contracts;
using Application.Dtos.CommentsDtos;
using Application.Dtos.ReactionsDto;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Value_Objects;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Services;

public class CommentService : ICommentService
{
	private readonly UserManager<BaseUser> _userManager;
	private readonly INotificationService _notificationService;
	private readonly IHateSpeechDetector _hateSpeechDetector;
	private readonly IUserClaimsService _userClaimsService;
	private readonly IRepositoryManager _repos;
	private readonly ICacheService _cacheService;
	private readonly IMapper _mapper;
	private readonly IStorageService? _storageService;
	private readonly ILogger<CommentService>? _logger;

	public CommentService(
		IRepositoryManager repos,
		IMapper mapper,
		IUserClaimsService userClaimsService,
		UserManager<BaseUser> userManager,
		INotificationService notificationService,
		IHateSpeechDetector hateSpeechDetector,
		ICacheService cacheService,
		ILogger<CommentService>? logger = null,
		IStorageService? storageService = null)
	{
		_repos = repos;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
		_userManager = userManager;
		_notificationService = notificationService;
		_hateSpeechDetector = hateSpeechDetector;
		_cacheService = cacheService;
		_logger = logger;
		_storageService = storageService;
	}

	public async Task<Result<IEnumerable<CommentResponse>>> GetCommentsByPostId(int postId)
	{
		var post = await _repos.Posts.GetPostById(postId, false);
		if (post is null ||
			post.IsDraft ||
			!await PostAccessPolicy.CanReadAsync(
				post,
				_userClaimsService.GetUserId(),
				_userClaimsService.GetRole(),
				_repos.ChatRooms))
		{
			return PostErrors.NotFound(postId);
		}

		var comments = await _repos.Comments
			.GetAllCommentsByPostId(postId, false);

		comments = comments
			.Select(comment =>
			{
				if (comment.Post.IsAnonymous &&
					comment.Post.AppUserId == comment.AppUserId)
				{
					comment.PhotoUrl = null;
					comment.Username = null;
					comment.AppUserId = null!;
				}
				return comment;
			})
			.ToList();

		var commentsResponse = _mapper.Map<IEnumerable<CommentResponse>>(comments).ToList();
		await PopulateReactionStatesAsync(commentsResponse);

		return commentsResponse;
	}

	public async Task<Result<CommentResponse>> CreateComment(int postId, CreateCommentRequest createCommentRequest)
	{
		if (!string.IsNullOrWhiteSpace(createCommentRequest.Content) &&
			await IsRestrictedContentAsync(createCommentRequest.Content, "comment"))
		{
			return Error.Forbidden("Content.Forbidden", "Your comment violates our policy against hate speech and could not be published");
		}

		var post = await _repos.Posts.GetPostById(postId, true);

		if (post is null ||
			post.IsDraft ||
			!await PostAccessPolicy.CanReadAsync(
				post,
				_userClaimsService.GetUserId(),
				_userClaimsService.GetRole(),
				_repos.ChatRooms))
		{
			return PostErrors.NotFound(postId);
		}

		var userId = _userClaimsService.GetUserId();
		var userName = _userClaimsService.GetUserName();
		var userRole = _userClaimsService.GetRole();
		var userPhotoUrl = _userClaimsService.GetPhotoUrl();

		if (!PostAccessPolicy.CanDiscuss(post, userId, userRole))
		{
			return Error.Forbidden("Users.Forbidden", "Only doctors and post author are allowed to comment on anonymous posts");
		}

		var mediaResult = await AttachmentMediaPolicy.ResolveAsync(
			_repos.PostMediaAssets,
			_storageService,
			createCommentRequest.MediaUrl,
			createCommentRequest.MediaType,
			userId,
			CommentErrors.InvalidMedia());
		if (mediaResult.IsFailure)
		{
			return mediaResult.Error;
		}

		var media = mediaResult.Value;

		var comment = _mapper.Map<Comment>(createCommentRequest);

		comment.AppUserId = userId;
		comment.PostId = postId;
		comment.PhotoUrl = userPhotoUrl;
		comment.CommentedAt = DateTime.UtcNow;
		comment.Username = userName;
		comment.MediaUrl = media?.Url;
		comment.MediaType = media?.MediaType;

		if (post.IsAnonymous && post.AppUserId == userId)
		{
			// Redact the response only; the internal author relationship must
			// remain for moderation, ownership and audit.
			comment.Username = null;
			comment.PhotoUrl = null;
		}

		_repos.Comments.CreateComment(comment);
		if (media is not null)
		{
			// Setting the navigation lets EF insert the comment and update the
			// asset's foreign key in the same unit of work.
			media.Comment = comment;
		}

		await _repos.SaveAsync();

		if (!post.AppUserId!.Equals(userId))
		{
			var notificationResources = new Dictionary<string, int>()
			{
				{ "postId", postId },
				{ "commentId", comment.Id }
			};

			var notification = Notification.CreateNotification(
				post.AppUserId!,
				"New Comment",
				$"{userName} has commented on your post",
				notificationResources,
				userName,
				userPhotoUrl,
				NotificationType.Comment,
				relatedResourceId: comment.Id,
				deduplicationKey: $"comment:{comment.Id}"
				);

			await _notificationService.SendNotificationAsync(notification);
		}

		var commentResponse = _mapper.Map<CommentResponse>(comment);
		RedactAnonymousAuthor(commentResponse, post);
		return commentResponse;
	}

	public async Task<Result<CommentResponse>> DeleteComment(int postId, int commentId)
	{
		var comment = await _repos.Comments.GetById(postId, commentId, true);
		if (comment is null)
		{
			return CommentErrors.NotFound(commentId);
		}

		var userId = _userClaimsService.GetUserId();
		var userRole = _userClaimsService.GetRole();
		if (!comment.AppUserId!.Equals(userId) && !PostAccessPolicy.IsModerator(userRole))
		{
			return CommentErrors.Forbidden(commentId);
		}

		var post = await _repos.Posts.GetPostById(postId, false);

		await DeleteTrackedMediaForCommentAsync(commentId);

		_repos.Comments.DeleteComment(comment);
		await _repos.SaveAsync();

		var commentResponse = _mapper.Map<CommentResponse>(comment);
		RedactAnonymousAuthor(commentResponse, post);
		return commentResponse;
	}

	public async Task<Result<CommentResponse>> UpdateComment(int postId, int commentId, UpdateCommentRequest updateCommentRequest)
	{
		if (!string.IsNullOrWhiteSpace(updateCommentRequest.Content) &&
			await IsRestrictedContentAsync(updateCommentRequest.Content, "comment"))
		{
			return Error.Forbidden("Content.Forbidden", "Your comment violates our policy against hate speech and could not be published");
		}

		var comment = await _repos.Comments.GetById(postId, commentId, true);
		if (comment is null)
		{
			return CommentErrors.NotFound(commentId);
		}

		var post = await _repos.Posts.GetPostById(postId, false);
		if (post is null ||
			post.IsDraft ||
			!await PostAccessPolicy.CanReadAsync(
				post,
				_userClaimsService.GetUserId(),
				_userClaimsService.GetRole(),
				_repos.ChatRooms))
		{
			return CommentErrors.NotFound(commentId);
		}

		var userId = _userClaimsService.GetUserId();

		if (!comment.AppUserId!.Equals(userId))
		{
			return PostErrors.Forbidden(postId);
		}

		// Media is fixed at creation time; the mapping profile ignores the
		// media fields so an edit cannot silently replace an attachment.
		_mapper.Map(updateCommentRequest, comment);

		_repos.Comments.UpdateComment(comment);
		await _repos.SaveAsync();

		var commentResponse = _mapper.Map<CommentResponse>(comment);
		await PopulateReactionStateAsync(commentResponse);
		RedactAnonymousAuthor(commentResponse, post);
		return commentResponse;
	}

	public async Task<Result<CommentResponse>> GetCommentById(int postId, int commentId)
	{
		var comment = await _repos.Comments.GetById(postId, commentId, false);
		if (comment is null)
		{
			return CommentErrors.NotFound(commentId);
		}

		// Comments are post content: the same audience rules have to apply on
		// this direct URL as on the post itself.
		var post = await _repos.Posts.GetPostById(postId, false);
		if (post is null ||
			post.IsDraft ||
			!await PostAccessPolicy.CanReadAsync(
				post,
				_userClaimsService.GetUserId(),
				_userClaimsService.GetRole(),
				_repos.ChatRooms))
		{
			return CommentErrors.NotFound(commentId);
		}

		var commentResponse = _mapper.Map<CommentResponse>(comment);
		await PopulateReactionStateAsync(commentResponse);
		RedactAnonymousAuthor(commentResponse, post);
		return commentResponse;
	}

	public async Task<Result<CommentResponse>> SetReaction(int postId, int commentId, string reaction)
	{
		if (!ReactionKinds.IsValid(reaction))
		{
			return Error.Validation(
				"Reactions.InvalidReaction",
				"Reaction must be one of: like, love, haha, wow, sad.");
		}

		var commentResult = await LoadReadableCommentAsync(postId, commentId);
		if (commentResult.IsFailure)
		{
			return commentResult.Error;
		}

		var userId = _userClaimsService.GetUserId();
		var normalized = ReactionKinds.Normalize(reaction);

		var existing = await _repos.Comments.GetReactionAsync(commentId, userId, true);
		if (existing is null)
		{
			_repos.Comments.AddReaction(new CommentReaction
			{
				CommentId = commentId,
				UserId = userId,
				Reaction = normalized,
				CreatedAtUtc = DateTime.UtcNow,
			});
		}
		else
		{
			existing.Reaction = normalized;
			existing.CreatedAtUtc = DateTime.UtcNow;
		}

		await _repos.SaveAsync();
		return await BuildReactionResponseAsync(commentResult.Value, userId);
	}

	public async Task<Result<CommentResponse>> RemoveReaction(int postId, int commentId)
	{
		var commentResult = await LoadReadableCommentAsync(postId, commentId);
		if (commentResult.IsFailure)
		{
			return commentResult.Error;
		}

		var userId = _userClaimsService.GetUserId();
		var existing = await _repos.Comments.GetReactionAsync(commentId, userId, true);
		if (existing is not null)
		{
			_repos.Comments.RemoveReaction(existing);
			await _repos.SaveAsync();
		}

		return await BuildReactionResponseAsync(commentResult.Value, userId);
	}

	private async Task<Result<Comment>> LoadReadableCommentAsync(int postId, int commentId)
	{
		var comment = await _repos.Comments.GetById(postId, commentId, false);
		if (comment is null)
		{
			return CommentErrors.NotFound(commentId);
		}

		var post = await _repos.Posts.GetPostById(postId, false);
		if (post is null ||
			post.IsDraft ||
			!await PostAccessPolicy.CanReadAsync(
				post,
				_userClaimsService.GetUserId(),
				_userClaimsService.GetRole(),
				_repos.ChatRooms))
		{
			return CommentErrors.NotFound(commentId);
		}

		return comment;
	}

	private async Task<Result<CommentResponse>> BuildReactionResponseAsync(Comment comment, string userId)
	{
		var commentResponse = _mapper.Map<CommentResponse>(comment);
		var state = await _repos.Comments.GetReactionsAsync(new[] { comment.Id }, false);
		ApplyReactionState(
			commentResponse,
			ReactionState.From(
				state.Select(row => (row.Reaction, row.UserId)),
				userId));

		var post = await _repos.Posts.GetPostById(comment.PostId, false);
		RedactAnonymousAuthor(commentResponse, post);
		return commentResponse;
	}

	private async Task PopulateReactionStateAsync(CommentResponse comment)
	{
		var reactions = await _repos.Comments.GetReactionsAsync(new[] { comment.Id }, false);
		ApplyReactionState(
			comment,
			ReactionState.From(
				reactions.Select(row => (row.Reaction, row.UserId)),
				_userClaimsService.GetUserId()));
	}

	private async Task PopulateReactionStatesAsync(IReadOnlyCollection<CommentResponse> comments)
	{
		if (comments.Count == 0)
		{
			return;
		}

		var userId = _userClaimsService.GetUserId();
		var reactions = await _repos.Comments.GetReactionsAsync(
			comments.Select(comment => comment.Id).ToArray(),
			false);
		var byComment = reactions
			.GroupBy(reaction => reaction.CommentId)
			.ToDictionary(group => group.Key, group => group.ToList());

		foreach (var comment in comments)
		{
			var rows = byComment.TryGetValue(comment.Id, out var commentRows)
				? commentRows
				: new List<CommentReaction>();

			ApplyReactionState(
				comment,
				ReactionState.From(
					rows.Select(row => (row.Reaction, row.UserId)),
					userId));
		}
	}

	private static void ApplyReactionState(CommentResponse response, ReactionState state)
	{
		response.Reactions = state.Counts
			.Select(count => new ReactionSummaryResponse
			{
				Reaction = count.Reaction,
				Count = count.Count
			})
			.ToList();
		response.MyReaction = state.MyReaction;
	}

	private async Task DeleteTrackedMediaForCommentAsync(int commentId)
	{
		// Deleting a comment also deletes its replies, so their attachments
		// have to be destroyed before the rows cascade away.
		var assets = await _repos.PostMediaAssets.GetAttachedToCommentThreadAsync(commentId, true);
		if (assets.Count == 0)
		{
			return;
		}

		if (_storageService is not null)
		{
			foreach (var asset in assets)
			{
				await _storageService.DeleteMediaAsync(asset.PublicId, asset.MediaType);
			}
		}

		foreach (var asset in assets)
		{
			_repos.PostMediaAssets.Remove(asset);
		}
	}

	private static void RedactAnonymousAuthor(CommentResponse comment, Post? post)
	{
		if (post is not null &&
			post.IsAnonymous &&
			string.Equals(post.AppUserId, comment.AppUserId, StringComparison.Ordinal))
		{
			comment.Username = null;
			comment.PhotoUrl = null;
			comment.AppUserId = null!;
		}
	}

	private async Task<bool> IsRestrictedContentAsync(string content, string contentType)
	{
		var result = await _hateSpeechDetector.IsHateSpeech(content);

		if (result.IsFailure)
		{
			_logger?.LogWarning(
				"Content moderation unavailable for a {ContentType}; allowing publication for review. {Code}: {Description}",
				contentType,
				result.Error.Code,
				result.Error.Description);
			return false;
		}

		return result.Value;
	}
}
