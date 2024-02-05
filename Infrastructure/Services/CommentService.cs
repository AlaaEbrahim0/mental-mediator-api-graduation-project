using System.IdentityModel.Tokens.Jwt;
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

    public async Task<Result<CommentResponse>> CreateComment(int postId, CreateCommentRequest createCommentRequest)
    {
        var post = await _repos.Posts.GetPostById(postId, true);
        if (post is null)
        {
            return PostErrors.NotFound(postId);
        }

        string userId = GetUserId();
        string userName = GetUserName();
        var comment = _mapper.Map<Comment>(createCommentRequest);

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

        var userId = GetUserId();
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

        var userId = GetUserId();

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

    private string GetUserId()
    {
        var userId = _signInManager.Context.User.FindFirst("uid")!.Value;
        return userId;
    }
    private string GetUserName()
    {
        var userName = _signInManager.Context.User.FindFirst(JwtRegisteredClaimNames.Name)!.Value;
        return userName;
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