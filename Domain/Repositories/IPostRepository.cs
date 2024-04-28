using Domain.Entities;
using Shared;

namespace Domain.Repositories;

public interface IPostRepository
{
	Task<IEnumerable<Post>> GetAllPosts(RequestParameters parameters, bool trackChanges);
	Task<IEnumerable<Post>> GetPostsByUserId(string userId, RequestParameters parameters, bool trackChanges);
	Task<Post?> GetPostById(int id, bool trackChanges);
	void CreatePost(Post post);
	void UpdatePost(Post post);
	void DeletePost(Post post);
}
