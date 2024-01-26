namespace Infrastructure.Contracts;
public interface IRepositoryManager
{
    IPostRepository Posts { get; }
    ICommentRepository Comments { get; }
    IReplyRepository Replies { get; }
    Task SaveAsync();
}
