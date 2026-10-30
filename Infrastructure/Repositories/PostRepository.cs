using System.Linq.Expressions;
using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.RequestParameters;

namespace Infrastructure.Repositories;

public class PostRepository : RepositoryBase<Post>, IPostRepository
{
	public PostRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public void CreatePost(Post post)
	{
		Create(post);
	}

	public void DeletePost(Post post)
	{
		Delete(post);
	}

	public async Task<IEnumerable<Post>> GetAllPosts(PostRequestParameters parameters, bool trackChanges, PostAudience? audience = null)
	{
		var query = ApplyAudience(FindAll(trackChanges), audience ?? PostAudience.Public);

		query = query
			.Where(p => !p.IsDraft)
			.Where(p => p.IsAnonymous == parameters.ConfessionsOnly);

		if (!string.IsNullOrEmpty(parameters.Title))
		{
			query = query.Where(p => p.Title.Contains(parameters.Title));
		}

		if (!string.IsNullOrEmpty(parameters.Content))
		{
			query = query.Where(p => p.Content.Contains(parameters.Content));
		}

		if (!string.IsNullOrEmpty(parameters.Username))
		{
			query = query.Where(x => string.Concat(x.AppUser.FirstName, " ", x.AppUser.LastName).Contains(parameters.Username));
		}

		if (parameters.StartTime != DateTime.MinValue)
		{
			query = query.Where(p => p.PostedOn >= parameters.StartTime);
		}

		if (parameters.EndTime != DateTime.MinValue)
		{
			query = query.Where(p => p.PostedOn <= parameters.EndTime);
		}

		return await query
			.OrderByDescending(c => c.PostedOn)
			.Select(PostProjection.Summary)
			.Paginate(parameters.PageNumber, parameters.PageSize)
			.ToListAsync();
	}


	public async Task<IEnumerable<Post>> GetConfessionOnly(PostRequestParameters parameters, bool trackChanges, PostAudience? audience = null)
	{
		var query = ApplyAudience(FindByCondition(x => x.IsAnonymous, trackChanges), audience ?? PostAudience.Public)
			.Where(p => !p.IsDraft);

		return await query
			.OrderByDescending(c => c.PostedOn)
			.Select(PostProjection.Summary)
			.Paginate(parameters.PageNumber, parameters.PageSize)
			.ToListAsync();
	}

	public async Task<IEnumerable<Post>> GetPostsByUserId(string userId, PostRequestParameters
			parameters, bool trackChanges)
	{
		// The service only reaches this method for the signed-in owner, so drafts
		// are included: this is the owner's workspace, not a public feed.
		return await FindByCondition(p => p.AppUserId == userId, trackChanges)
			.OrderByDescending(c => c.PostedOn)
			.Select(PostProjection.Summary)
			.Paginate(parameters.PageNumber, parameters.PageSize)
			.ToListAsync();
	}

	public async Task<Post?> GetPostById(int id, bool trackChanges)
	{
		if (!trackChanges)
		{
			return await FindByCondition(p => p.Id == id, false)
				.Select(PostProjection.Summary)
				.SingleOrDefaultAsync();
		}

		// Update and delete paths need the tracked entity itself. Returning a
		// projected instance and then calling Update() attaches a second entity
		// with the same key, which throws once tracking works correctly.
		var post = await FindByCondition(p => p.Id == id, true)
			.Include(p => p.AppUser)
			.SingleOrDefaultAsync();

		if (post is not null)
		{
			post.Username = post.IsAnonymous ? null : post.AppUser?.FullName;
			post.PhotoUrl = post.IsAnonymous ? null : post.AppUser?.PhotoUrl;
			post.CommentsCount = await _dbContext.Comments.CountAsync(comment => comment.PostId == id);
		}

		return post;
	}

	public async Task<Post?> GetLatestDraft(string userId, bool trackChanges)
	{
		return await FindByCondition(p => p.AppUserId == userId && p.IsDraft, trackChanges)
			.OrderByDescending(p => p.PostedOn)
			.Select(PostProjection.Summary)
			.FirstOrDefaultAsync();
	}

	public void UpdatePost(Post post)
	{
		Update(post);
	}

	public async Task<int> GetCount()
	{
		return await FindAll(false).CountAsync();
	}

	public async Task<(int Count, bool IsLiked)> GetLikeState(int postId, string? userId)
	{
		var query = _dbContext.PostLikes.Where(x => x.PostId == postId);
		return (await query.CountAsync(), userId != null && await query.AnyAsync(x => x.UserId == userId));
	}

	public async Task<IReadOnlyDictionary<int, (int Count, bool IsLiked)>> GetLikeStates(IEnumerable<int> postIds, string? userId)
	{
		var ids = postIds.Distinct().ToArray();
		if (ids.Length == 0)
		{
			return new Dictionary<int, (int Count, bool IsLiked)>();
		}

		var counts = await _dbContext.PostLikes
			.AsNoTracking()
			.Where(like => ids.Contains(like.PostId))
			.GroupBy(like => like.PostId)
			.Select(group => new { PostId = group.Key, Count = group.Count() })
			.ToListAsync();

		var liked = string.IsNullOrEmpty(userId)
			? new HashSet<int>()
			: (await _dbContext.PostLikes
				.AsNoTracking()
				.Where(like => ids.Contains(like.PostId) && like.UserId == userId)
				.Select(like => like.PostId)
				.ToListAsync())
				.ToHashSet();

		return ids.ToDictionary(
			id => id,
			id =>
			{
				var count = counts.FirstOrDefault(entry => entry.PostId == id)?.Count ?? 0;
				return (count, liked.Contains(id));
			});
	}

	public async Task SetLike(int postId, string userId, bool liked)
	{
		var existing = await _dbContext.PostLikes.FirstOrDefaultAsync(x => x.PostId == postId && x.UserId == userId);
		if (liked && existing == null) _dbContext.PostLikes.Add(new PostLike { PostId = postId, UserId = userId, LikedOn = DateTime.UtcNow });
		else if (!liked && existing != null) _dbContext.PostLikes.Remove(existing);
	}

	private static IQueryable<Post> ApplyAudience(IQueryable<Post> query, PostAudience audience)
	{
		return query.Where(post =>
			post.Visibility == PostVisibility.Public ||
			(audience.CanReadDoctorsOnly && post.Visibility == PostVisibility.DoctorsOnly) ||
			(post.Visibility == PostVisibility.SpecificGroup &&
				post.TargetChatRoomId != null &&
				audience.SpecificGroupIds.Contains(post.TargetChatRoomId.Value)));
	}
}

/// <summary>
/// One projection for every post read. Keeping it in a single expression means
/// a new post column cannot be silently dropped from one endpoint.
/// </summary>
internal static class PostProjection
{
	public static readonly Expression<Func<Post, Post>> Summary = post => new Post
	{
		Id = post.Id,
		AppUserId = post.AppUserId,
		Content = post.Content,
		Category = post.Category,
		Tags = post.Tags,
		PostedOn = post.PostedOn,
		Title = post.Title,
		IsAnonymous = post.IsAnonymous,
		IsDraft = post.IsDraft,
		Visibility = post.Visibility,
		TargetChatRoomId = post.TargetChatRoomId,
		MediaJson = post.MediaJson,
		Username = post.IsAnonymous ? null : post.AppUser!.FullName,
		PhotoUrl = post.IsAnonymous ? null : post.AppUser!.PhotoUrl,
		PostPhotoUrl = post.PostPhotoUrl,
		CommentsCount = post.Comments.Count()
	};
}
