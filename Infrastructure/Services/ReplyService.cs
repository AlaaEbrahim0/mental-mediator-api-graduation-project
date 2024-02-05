using System.IdentityModel.Tokens.Jwt;
using Application.Services;
using AutoMapper;
using Domain.Entities;
using Domain.Errors;
using Infrastructure.Contracts;
using Microsoft.AspNetCore.Identity;
using Shared;
using Shared.ReplyDtos;

namespace Infrastructure.Services;

public class ReplyService : IReplyService
{
    private readonly IRepositoryManager _repos;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly IMapper _mapper;

    public ReplyService(IMapper mapper, SignInManager<AppUser> signInManager, IRepositoryManager repos)
    {
        _mapper = mapper;
        _signInManager = signInManager;
        _repos = repos;
    }

    private string GetUserId()
    {
        return _signInManager.Context.User.FindFirst("uid")?.Value!;
    }
    private string GetUserName()
    {
        var userName = _signInManager.Context.User.FindFirst(JwtRegisteredClaimNames.Name)!.Value;
        return userName;
    }

    public async Task<Result<ReplyResponse>> CreateReply(int postId, int commentId, CreateReplyRequest createReplyRequest)
    {
        var comment = await _repos.Comments.GetById(postId, commentId, false);
        if (comment is null)
        {
            return CommentErrors.NotFound(commentId);
        }

        var userId = GetUserId();
        var userName = GetUserName();

        var reply = _mapper.Map<Reply>(createReplyRequest);

        reply.AppUserId = userId;
        reply.CommentId = comment.Id;
        reply.Username = userName;
        reply.RepliedAt = DateTime.UtcNow;

        _repos.Replies.CreateReply(reply);
        await _repos.SaveAsync();

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

        var userId = GetUserId();
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
        var comment = await _repos.Comments.GetById(postId, commentId, false);
        if (comment is null)
        {
            return CommentErrors.NotFound(commentId);
        }
        var replies = await _repos.Replies.GetRepliesByCommentId(commentId, false);

        var repliesResult = _mapper.Map<IEnumerable<ReplyResponse>>(replies);
        return repliesResult.ToList();

    }

    public async Task<Result<ReplyResponse>> UpdateReply(int postId, int commentId, int replyId, UpdateReplyRequest updateReplyRequest)
    {
        var reply = await _repos.Replies.GetById(postId, commentId, replyId, true);

        if (reply is null)
        {
            return ReplyErrors.NotFound(replyId);
        }

        var userId = GetUserId();
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
