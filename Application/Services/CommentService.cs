using Application.Contracts;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.CommentsDtos;

namespace Application.Services;

public class CommentService : ICommentService
{
	private readonly IRepositoryManager _repos;
	private readonly IMapper _mapper;
	private readonly IUserClaimsService _userClaimsService;
	private readonly UserManager<AppUser> _userManager;

	public CommentService(IRepositoryManager repos, IMapper mapper, IUserClaimsService userClaimsService, UserManager<AppUser> userManager)
	{
		_repos = repos;
		_mapper = mapper;
		_userClaimsService = userClaimsService;
		_userManager = userManager;
	}


	public async Task<Result<IEnumerable<CommentResponse>>> GetCommentsByPostId(int postId)
	{
		var comments = await _repos.Comments.GetAllCommentsByPostId(postId, false);
		var commentsResponse = _mapper.Map<IEnumerable<CommentResponse>>(comments);
		return commentsResponse.ToList();
	}

	public async Task<Result<CommentResponse>> CreateComment(int postId, CreateCommentRequest createCommentRequest)
	{
		var post = await _repos.Posts.GetPostById(postId, true);
		if (post is null)
		{
			return PostErrors.NotFound(postId);
		}

		var userId = _userClaimsService.GetUserId();
		var userName = _userClaimsService.GetUserName();
		var userRole = _userClaimsService.GetRole();

		var comment = _mapper.Map<Comment>(createCommentRequest);

		if (post.IsAnonymous && userRole != "Doctor")
		{
			return Error.Forbidden("Users.Forbidden", "Only doctors are allowed to comment on anonymous posts");
		}

		comment.AppUserId = userId;
		comment.PostId = postId;
		comment.CommentedAt = DateTime.UtcNow;
		comment.Username = userName;

		_repos.Comments.CreateComment(comment);
		await _repos.SaveAsync();

		var commentResponse = _mapper.Map<CommentResponse>(comment);
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
		if (!comment.AppUserId!.Equals(userId))
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
		var comment = await _repos.Comments.GetById(postId, commentId, true);
		if (comment is null)
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
		var commentResponse = _mapper.Map<CommentResponse>(comment);
		return commentResponse;
	}
}