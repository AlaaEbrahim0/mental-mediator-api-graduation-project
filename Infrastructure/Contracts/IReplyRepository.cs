using Domain.Entities;

namespace Infrastructure.Contracts;

public interface IReplyRepository
{
    Task<Reply?> GetById(int commentId, int postId, bool trackChangess);
    Task<IEnumerable<Reply?>> GetRepliesByCommendId(int commendId, bool trackChanges);
    void CreateReply(Reply Reply);
    void UpdateReply(Reply Reply);
    void DeleteReply(Reply Reply);
}
