using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class PostMediaAssetRepository : IPostMediaAssetRepository
{
	private readonly AppDbContext _dbContext;

	public PostMediaAssetRepository(AppDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	private IQueryable<PostMediaAsset> Assets(bool trackChanges) =>
		trackChanges ? _dbContext.PostMediaAssets : _dbContext.PostMediaAssets.AsNoTracking();

	public void Add(PostMediaAsset asset) => _dbContext.PostMediaAssets.Add(asset);

	public void Remove(PostMediaAsset asset) => _dbContext.PostMediaAssets.Remove(asset);

	public Task<PostMediaAsset?> GetByUrlAsync(string url, bool trackChanges) =>
		Assets(trackChanges).FirstOrDefaultAsync(asset => asset.Url == url);

	public async Task<IReadOnlyList<PostMediaAsset>> GetByUrlsAsync(IReadOnlyCollection<string> urls, bool trackChanges)
	{
		if (urls.Count == 0)
		{
			return Array.Empty<PostMediaAsset>();
		}

		return await Assets(trackChanges)
			.Where(asset => urls.Contains(asset.Url))
			.ToListAsync();
	}

	public async Task<IReadOnlyList<PostMediaAsset>> GetAttachedToPostAsync(int postId, bool trackChanges)
	{
		return await Assets(trackChanges)
			.Where(asset => asset.PostId == postId)
			.ToListAsync();
	}

	public async Task<IReadOnlyList<PostMediaAsset>> GetAttachedToCommentThreadAsync(int commentId, bool trackChanges)
	{
		return await Assets(trackChanges)
			.Where(asset =>
				asset.CommentId == commentId ||
				(asset.ReplyId != null && asset.Reply!.CommentId == commentId))
			.ToListAsync();
	}

	public async Task<IReadOnlyList<PostMediaAsset>> GetAttachedToReplyAsync(int replyId, bool trackChanges)
	{
		return await Assets(trackChanges)
			.Where(asset => asset.ReplyId == replyId)
			.ToListAsync();
	}

	public async Task<IReadOnlyList<PostMediaAsset>> GetAttachedToPostThreadAsync(int postId, bool trackChanges)
	{
		return await Assets(trackChanges)
			.Where(asset =>
				asset.PostId == postId ||
				(asset.CommentId != null && asset.Comment!.PostId == postId) ||
				(asset.ReplyId != null && asset.Reply!.Comment!.PostId == postId))
			.ToListAsync();
	}

	public async Task<IReadOnlyList<PostMediaAsset>> GetOrphansAsync(
		string ownerId,
		DateTime olderThanUtc,
		int take,
		bool trackChanges)
	{
		return await Assets(trackChanges)
			.Where(asset =>
				asset.OwnerId == ownerId &&
				asset.PostId == null &&
				asset.CommentId == null &&
				asset.ReplyId == null &&
				asset.CreatedAtUtc < olderThanUtc)
			.OrderBy(asset => asset.CreatedAtUtc)
			.Take(take)
			.ToListAsync();
	}
}
