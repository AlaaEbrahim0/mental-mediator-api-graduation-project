using Domain.Entities;
using Shared.RequestParameters;

namespace Domain.Repositories;

/// <summary>
/// The audience the caller may see. It is resolved in the service from the
/// authenticated role and active room memberships, never from query input.
/// </summary>
public sealed record PostAudience(bool CanReadDoctorsOnly, int[] SpecificGroupIds)
{
	public static readonly PostAudience Public = new(false, Array.Empty<int>());
}

public interface IPostRepository
{
	Task<IEnumerable<Post>> GetAllPosts(PostRequestParameters parameters, bool trackChanges, PostAudience? audience = null);
	Task<IEnumerable<Post>> GetConfessionOnly(PostRequestParameters parameters, bool trackChanges, PostAudience? audience = null);
	Task<IEnumerable<Post>> GetPostsByUserId(string userId, PostRequestParameters parameters, bool trackChanges);
	Task<Post?> GetPostById(int id, bool trackChanges);
	Task<Post?> GetLatestDraft(string userId, bool trackChanges);
	Task<int> GetCount();
	Task<(int Count, bool IsLiked)> GetLikeState(int postId, string? userId);

	/// <summary>Like counts and the caller's liked state for a page in two queries.</summary>
	Task<IReadOnlyDictionary<int, (int Count, bool IsLiked)>> GetLikeStates(IEnumerable<int> postIds, string? userId);

	Task SetLike(int postId, string userId, bool liked);
	void CreatePost(Post post);
	void UpdatePost(Post post);
	void DeletePost(Post post);
}
