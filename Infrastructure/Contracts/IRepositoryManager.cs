using Infrastructure.Data;

namespace Infrastructure.Contracts;
public interface IRepositoryManager
{
    IPostRepository PostRepository { get; }
    Task SaveAsync();
}

