using Domain.Entities;

namespace Infrastructure.Contracts;

public interface IPostRepository
{
    Task<IEnumerable<Post>> GetAllPosts(bool trackChanges);
    Task<Post?> GetPostById(int id, bool trackChanges);
    void CreatePost(Post post);
    void UpdatePost(Post post);
    void DeletePost(Post post);
}
