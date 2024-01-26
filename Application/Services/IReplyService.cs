using Shared;
using Shared.ReplyDtos;

namespace Application.Services;
public interface IReplyService
{
    Task<Result<IEnumerable<ReplyResponse>>> GetRepliesForComment(int postId, int commentId);
    Task<Result<string>> CreateReply(int postId, int commentId, CreateReplyRequest createReplyRequest);
    Task<Result<string>> UpdateReply(int postId, int commentId, int replyId, UpdateReplyRequest updateReplyRequest);
    Task<Result<string>> DeleteReply(int postId, int commentId, int replyId);
}
