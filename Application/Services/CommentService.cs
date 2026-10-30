using Application.Contracts;
using Application.Dtos.CommentsDtos;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Application.Community;
using Domain.Errors;
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
	private readonly ILogger<CommentService>? _logger;

	public CommentService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService, UserManager<BaseUser> userManager, INotificationService notificationService, IHateSpeechDetector hateSpeechDetector, ICacheService cacheService, ILogger<CommentService>? logger = null)
	{
		_repos = repos;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
		_userManager = userManager;
		_notificationService = notificationService;
		_hateSpeechDetector = hateSpeechDetector;
		_cacheService = cacheService;
		_logger = logger;
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
				}
				return comment;
			})
			.ToList();

		var commentsResponse = _mapper.Map<IEnumerable<CommentResponse>>(comments);

		return commentsResponse.ToList();
	}

	public async Task<Result<CommentResponse>> CreateComment(int postId, CreateCommentRequest createCommentRequest)
	{
		if (await IsRestrictedContentAsync(createCommentRequest.Content!, "comment"))
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

		var comment = _mapper.Map<Comment>(createCommentRequest);

		comment.AppUserId = userId;
		comment.PostId = postId;
		comment.PhotoUrl = userPhotoUrl;
		comment.CommentedAt = DateTime.UtcNow;
		comment.Username = userName;

		if (post.IsAnonymous && post.AppUserId == userId)
		{
			comment.Username = null;
			comment.PhotoUrl = null;
		}

		_repos.Comments.CreateComment(comment);
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
		return commentResponse;
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

		_repos.Comments.DeleteComment(comment);
		await _repos.SaveAsync();


		var commentResponse = _mapper.Map<CommentResponse>(comment);
		return commentResponse;
	}

	public async Task<Result<CommentResponse>> UpdateComment(int postId, int commentId, UpdateCommentRequest updateCommentRequest)
	{
		if (await IsRestrictedContentAsync(updateCommentRequest.Content!, "comment"))
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

		_mapper.Map(updateCommentRequest, comment);

		_repos.Comments.UpdateComment(comment);
		await _repos.SaveAsync();

		var commentResponse = _mapper.Map<CommentResponse>(comment);
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
		return commentResponse;
	}
}