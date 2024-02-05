using Domain.Repositories;

namespace Application.Contracts;
public interface IRepositoryManager
{
    IPostRepository Posts { get; }
    ICommentRepository Comments { get; }
    IReplyRepository Replies { get; }
    Task SaveAsync();
}
