using Application.Dtos.ReplyDtos;
using Shared;

namespace Application.Services;
public interface IReplyService
{
    Task<Result<ReplyResponse>> GetReplyById(int postId, int commentId, int replyId);
    Task<Result<IEnumerable<ReplyResponse>>> GetRepliesForComment(int postId, int commentId);
    Task<Result<ReplyResponse>> CreateReply(int postId, int commentId, CreateReplyRequest createReplyRequest);
    Task<Result<ReplyResponse>> UpdateReply(int postId, int commentId, int replyId, UpdateReplyRequest updateReplyRequest);
    Task<Result<ReplyResponse>> DeleteReply(int postId, int commentId, int replyId);
}
