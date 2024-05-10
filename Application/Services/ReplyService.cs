using Application.Contracts;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Domain.Errors;
using Shared;
using Shared.ReplyDtos;

namespace Application.Services;

public class ReplyService : IReplyService
{
	private readonly IRepositoryManager _repos;
	private readonly IMapper _mapper;
	private readonly IUserClaimsService _userClaimsService;
	private readonly INotificationService _notificationService;
	private readonly IHateSpeechDetector _hateSpeechDetector;
	private readonly ICacheService _cacheService;

	public ReplyService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService, INotificationService notificationService, IHateSpeechDetector hateSpeechDetector, ICacheService cacheService)
	{
		_repos = repos;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
		_notificationService = notificationService;
		_hateSpeechDetector = hateSpeechDetector;
		_cacheService = cacheService;
	}

	public async Task<Result<ReplyResponse>> CreateReply(int postId, int commentId, CreateReplyRequest createReplyRequest)
	{
		var comment = await _repos.Comments.GetById(postId, commentId, false);
		if (comment is null)
		{
			return CommentErrors.NotFound(commentId);
		}

		var isHateSpeechResult = await _hateSpeechDetector.IsHateSpeech(createReplyRequest.Content!);

		if (isHateSpeechResult.IsFailure)
		{
			return isHateSpeechResult.Error;
		}
		if (isHateSpeechResult.Value)
		{
			return Error.Forbidden("Content.Forbidden", "Your reply violates our policy against hate speech and could not be published");
		}

		var userId = _userClaimsService.GetUserId();
		var userName = _userClaimsService.GetUserName();

		var reply = _mapper.Map<Reply>(createReplyRequest);

		reply.AppUserId = userId;
		reply.CommentId = comment.Id;
		reply.Username = userName;
		reply.RepliedAt = DateTime.UtcNow;

		_repos.Replies.CreateReply(reply);
		await _repos.SaveAsync();

		if (!comment.AppUserId!.Equals(userId))
		{
			var replyResources = new Dictionary<string, int>()
			{
				{ "postId", postId },
				{ "commentId", comment.Id },
				{ "replyId", reply.Id },
			};

			var notification = Notification.CreateNotification(
				comment.AppUserId!,
				$"{userName} has replied to your comment",
				replyResources,
				NotificationType.Reply
				);

			_repos.Notifications.CreateNotification(notification);
			await _repos.SaveAsync();

			await _notificationService.SendNotificationAsync(notification);
		}


		var replyResponse = _mapper.Map<ReplyResponse>(reply);
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
		if (!reply.AppUserId!.Equals(userId))
		{
			return ReplyErrors.Forbidden(replyId);
		}

		_repos.Replies.DeleteReply(reply);
		await _repos.SaveAsync();

		var replyResponse = _mapper.Map<ReplyResponse>(reply);
		return replyResponse;
	}

	public async Task<Result<IEnumerable<ReplyResponse>>> GetRepliesForComment(int postId, int commentId)
	{
		var cachedReplies = await _cacheService.GetAsync<List<ReplyResponse>>("replies");

		var comment = await _repos.Comments.GetById(postId, commentId, false);
		if (comment is null)
		{
			return CommentErrors.NotFound(commentId);
		}
		var replies = await _repos.Replies.GetRepliesByCommentId(commentId, false);

		var repliesResult = _mapper.Map<IEnumerable<ReplyResponse>>(replies);
		//await _cacheService.SetAsync("replies", repliesResult);

		return repliesResult.ToList();

	}

	public async Task<Result<ReplyResponse>> UpdateReply(int postId, int commentId, int replyId, UpdateReplyRequest updateReplyRequest)
	{
		var reply = await _repos.Replies.GetById(postId, commentId, replyId, true);

		if (reply is null)
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
		return replyResponse;
	}

	public async Task<Result<ReplyResponse>> GetReplyById(int postId, int commentId, int replyId)
	{
		var reply = await _repos.Replies.GetById(postId, commentId, replyId, false);
		if (reply is null)
		{
			return ReplyErrors.NotFound(replyId);
		}

		var replyResponse = _mapper.Map<ReplyResponse>(reply);
		return replyResponse;
	}
}
