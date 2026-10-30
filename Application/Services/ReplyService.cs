using Application.Community;
using Application.Contracts;
using Application.Dtos.ReplyDtos;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
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
	private readonly ILogger<ReplyService>? _logger;

	public ReplyService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService, INotificationService notificationService, IHateSpeechDetector hateSpeechDetector, ICacheService cacheService, ILogger<ReplyService>? logger = null)
	{
		_repos = repos;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
		_notificationService = notificationService;
		_hateSpeechDetector = hateSpeechDetector;
		_cacheService = cacheService;
		_logger = logger;
	}

	public async Task<Result<ReplyResponse>> CreateReply(int postId, int commentId, CreateReplyRequest createReplyRequest)
	{
		if (await IsRestrictedContentAsync(createReplyRequest.Content!, "reply"))
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

		var reply = _mapper.Map<Reply>(createReplyRequest);

		reply.AppUserId = userId;
		reply.CommentId = comment.Id;
		reply.Username = userName;
		reply.RepliedAt = DateTime.UtcNow;

		_repos.Replies.CreateReply(reply);
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
		foreach (var reply in repliesResult)
		{
			RedactAnonymousAuthor(reply, post);
		}
		return repliesResult;
	}

	public async Task<Result<ReplyResponse>> UpdateReply(int postId, int commentId, int replyId, UpdateReplyRequest updateReplyRequest)
	{
		if (await IsRestrictedContentAsync(updateReplyRequest.Content!, "reply"))
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

		_mapper.Map(updateReplyRequest, reply);

		_repos.Replies.UpdateReply(reply);
		await _repos.SaveAsync();

		var replyResponse = _mapper.Map<ReplyResponse>(reply);
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
		RedactAnonymousAuthor(replyResponse, post);
		return replyResponse;
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
