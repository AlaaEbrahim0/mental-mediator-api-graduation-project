using Domain.Entities;
using Shared;

namespace Domain.Repositories;

public interface IPostRepository
{
	Task<IEnumerable<Post>> GetAllPosts(PostRequestParameters parameters, bool trackChanges);
	Task<IEnumerable<Post>> GetConfessionOnly(PostRequestParameters parameters, bool trackChanges);
	Task<IEnumerable<Post>> GetPostsByUserId(string userId, PostRequestParameters parameters, bool trackChanges);
	Task<Post?> GetPostById(int id, bool trackChanges);
	Task<int> GetCount();
	void CreatePost(Post post);
	void UpdatePost(Post post);
	void DeletePost(Post post);
}
