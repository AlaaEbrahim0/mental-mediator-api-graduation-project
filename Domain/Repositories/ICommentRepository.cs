using Domain.Entities;

namespace Domain.Repositories;
public interface ICommentRepository
{
    Task<IEnumerable<Comment>> GetAllCommentsByPostId(int postId, bool trackChanges);
    Task<Comment?> GetById(int postId, int commentId, bool trackChanges);
    void CreateComment(Comment comment);
    void UpdateComment(Comment comment);
    void DeleteComment(Comment comment);

}
