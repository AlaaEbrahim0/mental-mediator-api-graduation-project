using Domain.Entities;

namespace Domain.Repositories;
public interface ICommentRepository
{
	Task<IEnumerable<Comment>> GetAllCommentsByPostId(int postId, bool trackChanges);
	Task<Comment?> GetById(int postId, int commentId, bool trackChanges);
	Task<IReadOnlyList<CommentReaction>> GetReactionsAsync(IReadOnlyCollection<int> commentIds, bool trackChanges);
	Task<CommentReaction?> GetReactionAsync(int commentId, string userId, bool trackChanges);
	void AddReaction(CommentReaction reaction);
	void RemoveReaction(CommentReaction reaction);
	void CreateComment(Comment comment);
	void UpdateComment(Comment comment);
	void DeleteComment(Comment comment);
}
