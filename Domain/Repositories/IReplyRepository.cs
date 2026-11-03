using Domain.Entities;

namespace Domain.Repositories;

public interface IReplyRepository
{
    Task<Reply?> GetById(int postId, int commentId, int replyId, bool trackChanges);
    Task<IEnumerable<Reply?>> GetRepliesByCommentId(int commentId, bool trackChanges);
    Task<IReadOnlyList<ReplyReaction>> GetReactionsAsync(IReadOnlyCollection<int> replyIds, bool trackChanges);
    Task<ReplyReaction?> GetReactionAsync(int replyId, string userId, bool trackChanges);
    void AddReaction(ReplyReaction reaction);
    void RemoveReaction(ReplyReaction reaction);
    void CreateReply(Reply Reply);
    void UpdateReply(Reply Reply);
    void DeleteReply(Reply Reply);
}
