﻿using Shared;
using Shared.ReplyDtos;

namespace Application.Services;
public interface IReplyService
{
    Task<Result<ReplyResponse>> GetReplyById(int postId, int commentId, int replyId);
    Task<Result<IEnumerable<ReplyResponse>>> GetRepliesForComment(int postId, int commentId);
    Task<Result<ReplyResponse>> CreateReply(int postId, int commentId, CreateReplyRequest createReplyRequest);
    Task<Result<string>> UpdateReply(int postId, int commentId, int replyId, UpdateReplyRequest updateReplyRequest);
    Task<Result<string>> DeleteReply(int postId, int commentId, int replyId);
}
