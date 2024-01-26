using Domain.Entities;

namespace Infrastructure.Contracts;

public interface IReplyRepository
{
    Task<Reply?> GetById(int postId, int commentId, int replyId, bool trackChanges);
    Task<IEnumerable<Reply?>> GetRepliesByCommentId(int commentId, bool trackChanges);
    void CreateReply(Reply Reply);
    void UpdateReply(Reply Reply);
    void DeleteReply(Reply Reply);
}
