using Application.Community;
using Application.Contracts;
using Application.Dtos.ReactionsDto;
using Application.Dtos.ReplyDtos;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Domain.Value_Objects;
using Microsoft.Extensions.Logging;
using Shared;

namespace Application.Services;

public class ReplyService : IReplyService
{
	private readonly IRepositoryManager _repos;
	private readonly IMapper _mapper;
	private readonly IUserClaimsService _userClaimsService;
	private readonly INotificationService _notificationService;
	private readonly IHateSpeechDetector _hateSpeechDetector;
	private readonly ICacheService _cacheService;
	private readonly IStorageService? _storageService;
	private readonly ILogger<ReplyService>? _logger;

	public ReplyService(
		IRepositoryManager repos,
		IMapper mapper,
		IUserClaimsService userClaimsService,
		INotificationService notificationService,
		IHateSpeechDetector hateSpeechDetector,
		ICacheService cacheService,
		ILogger<ReplyService>? logger = null,
		IStorageService? storageService = null)
	{
		_repos = repos;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
		_notificationService = notificationService;
		_hateSpeechDetector = hateSpeechDetector;
		_cacheService = cacheService;
		_logger = logger;
		_storageService = storageService;
	}

	public async Task<Result<ReplyResponse>> CreateReply(int postId, int commentId, CreateReplyRequest createReplyRequest)
	{
		if (!string.IsNullOrWhiteSpace(createReplyRequest.Content) &&
			await IsRestrictedContentAsync(createReplyRequest.Content, "reply"))
		{
			return Error.Forbidden("Content.Forbidden", "Your reply violates our policy against hate speech and could not be published");
		}

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
			return PostErrors.NotFound(postId);
		}

		if (!PostAccessPolicy.CanDiscuss(
				post,
				_userClaimsService.GetUserId(),
				_userClaimsService.GetRole()))
		{
			return Error.Forbidden(
				"Users.Forbidden",
				"Only doctors and the post author can reply on anonymous posts");
		}

		var userId = _userClaimsService.GetUserId();
		var userName = _userClaimsService.GetUserName();
		var userPhotoUrl = _userClaimsService.GetPhotoUrl();

		var mediaResult = await AttachmentMediaPolicy.ResolveAsync(
			_repos.PostMediaAssets,
			_storageService,
			createReplyRequest.MediaUrl,
			createReplyRequest.MediaType,
			userId,
			ReplyErrors.InvalidMedia());
		if (mediaResult.IsFailure)
		{
			return mediaResult.Error;
		}

		var media = mediaResult.Value;

		var reply = _mapper.Map<Reply>(createReplyRequest);

		reply.AppUserId = userId;
		reply.CommentId = comment.Id;
		reply.Username = userName;
		reply.RepliedAt = DateTime.UtcNow;
		reply.MediaUrl = media?.Url;
		reply.MediaType = media?.MediaType;

		_repos.Replies.CreateReply(reply);
		if (media is not null)
		{
			// Setting the navigation lets EF insert the reply and update the
			// asset's foreign key in the same unit of work.
			media.Reply = reply;
		}

		await _repos.SaveAsync();

		if (!comment.AppUserId!.Equals(userId))
		{
			// The anonymous post author must stay anonymous even in the
			// notification another community member receives.
			var isAnonymousAuthor = post.IsAnonymous &&
				string.Equals(post.AppUserId, userId, StringComparison.Ordinal);
			var notifierName = isAnonymousAuthor ? "Anonymous" : userName;
			var notifierPhotoUrl = isAnonymousAuthor ? null : userPhotoUrl;

			var replyResources = new Dictionary<string, int>()
			{
				{ "postId", postId },
				{ "commentId", comment.Id },
				{ "replyId", reply.Id },
			};

			var notification = Notification.CreateNotification(
				comment.AppUserId!,
				"New Reply",
				$"{notifierName} has replied to your comment",
				replyResources,
				notifierName,
				notifierPhotoUrl ?? string.Empty,
				NotificationType.Reply,
				relatedResourceId: reply.Id,
				deduplicationKey: $"reply:{reply.Id}"
				);

			await _notificationService.SendNotificationAsync(notification);
		}

		var replyResponse = _mapper.Map<ReplyResponse>(reply);
		RedactAnonymousAuthor(replyResponse, post);
		return replyResponse;
	}

	public async Task<Result<ReplyResponse>> DeleteReply(int postId, int commentId, int replyId)
	{
		var reply = await _repos.Replies.GetById(postId, commentId, replyId, true);

		if (reply is null)
		{
			return ReplyErrors.NotFound(replyId);
		}

		var userId = _userClaimsService.GetUserId();
		var userRole = _userClaimsService.GetRole();

		if (!reply.AppUserId!.Equals(userId) && !PostAccessPolicy.IsModerator(userRole))
		{
			return ReplyErrors.Forbidden(replyId);
		}

		var post = await _repos.Posts.GetPostById(postId, false);

		await DeleteTrackedMediaForReplyAsync(replyId);

		_repos.Replies.DeleteReply(reply);
		await _repos.SaveAsync();

		var replyResponse = _mapper.Map<ReplyResponse>(reply);
		RedactAnonymousAuthor(replyResponse, post);
		return replyResponse;
	}

	public async Task<Result<IEnumerable<ReplyResponse>>> GetRepliesForComment(int postId, int commentId)
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

		var replies = await _repos.Replies.GetRepliesByCommentId(commentId, false);

		var repliesResult = _mapper.Map<IEnumerable<ReplyResponse>>(replies).ToList();
		await PopulateReactionStatesAsync(repliesResult);
		foreach (var reply in repliesResult)
		{
			RedactAnonymousAuthor(reply, post);
		}
		return repliesResult;
	}

	public async Task<Result<ReplyResponse>> UpdateReply(int postId, int commentId, int replyId, UpdateReplyRequest updateReplyRequest)
	{
		if (!string.IsNullOrWhiteSpace(updateReplyRequest.Content) &&
			await IsRestrictedContentAsync(updateReplyRequest.Content, "reply"))
		{
			return Error.Forbidden("Content.Forbidden", "Your reply violates our policy against hate speech and could not be published");
		}

		var reply = await _repos.Replies.GetById(postId, commentId, replyId, true);

		if (reply is null)
		{
			return ReplyErrors.NotFound(replyId);
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
			return ReplyErrors.NotFound(replyId);
		}

		var userId = _userClaimsService.GetUserId();
		if (!reply.AppUserId!.Equals(userId))
		{
			return ReplyErrors.Forbidden(replyId);
		}

		// Media is fixed at creation time; the mapping profile ignores the
		// media fields so an edit cannot silently replace an attachment.
		_mapper.Map(updateReplyRequest, reply);

		_repos.Replies.UpdateReply(reply);
		await _repos.SaveAsync();

		var replyResponse = _mapper.Map<ReplyResponse>(reply);
		await PopulateReactionStateAsync(replyResponse);
		RedactAnonymousAuthor(replyResponse, post);
		return replyResponse;
	}

	public async Task<Result<ReplyResponse>> GetReplyById(int postId, int commentId, int replyId)
	{
		var reply = await _repos.Replies.GetById(postId, commentId, replyId, false);
		if (reply is null)
		{
			return ReplyErrors.NotFound(replyId);
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
			return ReplyErrors.NotFound(replyId);
		}

		var replyResponse = _mapper.Map<ReplyResponse>(reply);
		await PopulateReactionStateAsync(replyResponse);
		RedactAnonymousAuthor(replyResponse, post);
		return replyResponse;
	}

	public async Task<Result<ReplyResponse>> SetReaction(int postId, int commentId, int replyId, string reaction)
	{
		if (!ReactionKinds.IsValid(reaction))
		{
			return Error.Validation(
				"Reactions.InvalidReaction",
				"Reaction must be one of: like, love, haha, wow, sad.");
		}

		var replyResult = await LoadReadableReplyAsync(postId, commentId, replyId);
		if (replyResult.IsFailure)
		{
			return replyResult.Error;
		}

		var userId = _userClaimsService.GetUserId();
		var normalized = ReactionKinds.Normalize(reaction);

		var existing = await _repos.Replies.GetReactionAsync(replyId, userId, true);
		if (existing is null)
		{
			_repos.Replies.AddReaction(new ReplyReaction
			{
				ReplyId = replyId,
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
		return await BuildReactionResponseAsync(postId, replyResult.Value, userId);
	}

	public async Task<Result<ReplyResponse>> RemoveReaction(int postId, int commentId, int replyId)
	{
		var replyResult = await LoadReadableReplyAsync(postId, commentId, replyId);
		if (replyResult.IsFailure)
		{
			return replyResult.Error;
		}

		var userId = _userClaimsService.GetUserId();
		var existing = await _repos.Replies.GetReactionAsync(replyId, userId, true);
		if (existing is not null)
		{
			_repos.Replies.RemoveReaction(existing);
			await _repos.SaveAsync();
		}

		return await BuildReactionResponseAsync(postId, replyResult.Value, userId);
	}

	private async Task<Result<Reply>> LoadReadableReplyAsync(int postId, int commentId, int replyId)
	{
		var reply = await _repos.Replies.GetById(postId, commentId, replyId, false);
		if (reply is null)
		{
			return ReplyErrors.NotFound(replyId);
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
			return ReplyErrors.NotFound(replyId);
		}

		return reply;
	}

	private async Task<Result<ReplyResponse>> BuildReactionResponseAsync(int postId, Reply reply, string userId)
	{
		var replyResponse = _mapper.Map<ReplyResponse>(reply);
		var state = await _repos.Replies.GetReactionsAsync(new[] { reply.Id }, false);
		ApplyReactionState(
			replyResponse,
			ReactionState.From(
				state.Select(row => (row.Reaction, row.UserId)),
				userId));

		var post = await _repos.Posts.GetPostById(postId, false);
		RedactAnonymousAuthor(replyResponse, post);
		return replyResponse;
	}

	private async Task PopulateReactionStateAsync(ReplyResponse reply)
	{
		var reactions = await _repos.Replies.GetReactionsAsync(new[] { reply.Id }, false);
		ApplyReactionState(
			reply,
			ReactionState.From(
				reactions.Select(row => (row.Reaction, row.UserId)),
				_userClaimsService.GetUserId()));
	}

	private async Task PopulateReactionStatesAsync(IReadOnlyCollection<ReplyResponse> replies)
	{
		if (replies.Count == 0)
		{
			return;
		}

		var userId = _userClaimsService.GetUserId();
		var reactions = await _repos.Replies.GetReactionsAsync(
			replies.Select(reply => reply.Id).ToArray(),
			false);
		var byReply = reactions
			.GroupBy(reaction => reaction.ReplyId)
			.ToDictionary(group => group.Key, group => group.ToList());

		foreach (var reply in replies)
		{
			var rows = byReply.TryGetValue(reply.Id, out var replyRows)
				? replyRows
				: new List<ReplyReaction>();

			ApplyReactionState(
				reply,
				ReactionState.From(
					rows.Select(row => (row.Reaction, row.UserId)),
					userId));
		}
	}

	private static void ApplyReactionState(ReplyResponse response, ReactionState state)
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

	private async Task DeleteTrackedMediaForReplyAsync(int replyId)
	{
		var assets = await _repos.PostMediaAssets.GetAttachedToReplyAsync(replyId, true);
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

	private static void RedactAnonymousAuthor(ReplyResponse reply, Post? post)
	{
		if (post is not null &&
			post.IsAnonymous &&
			string.Equals(post.AppUserId, reply.AppUserId, StringComparison.Ordinal))
		{
			reply.Username = null;
			reply.PhotoUrl = null;
			reply.AppUserId = null;
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
