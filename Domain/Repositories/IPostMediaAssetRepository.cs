using Domain.Entities;

namespace Domain.Repositories;

public interface IPostMediaAssetRepository
{
	void Add(PostMediaAsset asset);
	void Remove(PostMediaAsset asset);
	Task<PostMediaAsset?> GetByUrlAsync(string url, bool trackChanges);
	Task<IReadOnlyList<PostMediaAsset>> GetByUrlsAsync(IReadOnlyCollection<string> urls, bool trackChanges);
	Task<IReadOnlyList<PostMediaAsset>> GetAttachedToPostAsync(int postId, bool trackChanges);
	Task<IReadOnlyList<PostMediaAsset>> GetOrphansAsync(string ownerId, DateTime olderThanUtc, int take, bool trackChanges);
}
