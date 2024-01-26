using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Infrastructure.Contracts;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.CommentsDtos;

namespace Infrastructure.Services;

public class CommentService : ICommentService
{
    private readonly IRepositoryManager _repos;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IMapper _mapper;

    public CommentService(IMapper mapper, SignInManager<AppUser> signInManager, IRepositoryManager repos)
    {
        _mapper = mapper;
        _signInManager = signInManager;
        _repos = repos;
    }

    public async Task<Result<IEnumerable<CommentResponse>>> GetCommentsByPostId(int postId)
    {
        var comments = await _repos.Comments.GetAllCommentsByPostId(postId, false);
        var commentsResponse = _mapper.Map<IEnumerable<CommentResponse>>(comments);
        return commentsResponse.ToList();
    }

    public async Task<Result<string>> CreateComment(int postId, CreateCommentRequest createCommentRequest)
    {
        var post = await _repos.Posts.GetPostById(postId, true);
        if (post is null)
        {
            return PostErrors.NotFound(postId);
        }

        string userId = GetUserId();
        var comment = _mapper.Map<Comment>(createCommentRequest);

        comment.AppUserId = userId;
        comment.PostId = postId;
        comment.CommentedAt = DateTime.UtcNow;

        _repos.Comments.CreateComment(comment);
        await _repos.SaveAsync();

        return "commment has been created successfully";
    }

    public async Task<Result<string>> DeleteComment(int postId, int commentId)
    {
        var comment = await _repos.Comments.GetById(postId, commentId, true);
        if (comment is null)
        {
            return CommentErrors.NotFound(commentId);
        }

        var userId = GetUserId();
        if (!comment.AppUserId!.Equals(userId))
        {
            return CommentErrors.Forbidden(commentId);
        }

        _repos.Comments.DeleteComment(comment);
        await _repos.SaveAsync();

        return "comment has been deleted successfully";
    }

    public async Task<Result<string>> UpdateComment(int postId, int commentId, UpdateCommentRequest updateCommentRequest)
    {
        var comment = await _repos.Comments.GetById(postId, commentId, true);
        if (comment is null)
        {
            return CommentErrors.NotFound(commentId);
        }

        var userId = GetUserId();

        if (!comment.AppUserId!.Equals(userId))
        {
            return PostErrors.Forbidden(postId);
        }

        _mapper.Map(updateCommentRequest, comment);

        _repos.Comments.UpdateComment(comment);
        await _repos.SaveAsync();

        return "comment has been updated successfully";
    }

    private string GetUserId()
    {
        return _signInManager.Context.User.FindFirst("uid")!.Value;
    }
}