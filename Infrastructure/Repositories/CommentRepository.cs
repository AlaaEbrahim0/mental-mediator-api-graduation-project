using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
public class CommentRepository : RepositoryBase<Comment>, ICommentRepository
{
	public CommentRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public void UpdateComment(Comment comment)
	{
		Update(comment);
	}

	public void CreateComment(Comment comment)
	{
		Create(comment);
	}

	public void DeleteComment(Comment comment)
	{
		Delete(comment);
	}

	public async Task<IEnumerable<Comment>> GetAllCommentsByPostId(int postId, bool trackChanges)
	{
		return await FindByCondition(c => c.PostId == postId, trackChanges)
			.Include(c => c.AppUser)
			.Include(c => c.Post)
			.Select(x => new Comment
			{
				Id = x.Id,
				AppUserId = x.AppUserId,
				Username = x.AppUser.FullName,
				PostId = x.PostId,
				CommentedAt = x.CommentedAt,
				Content = x.Content,
				MediaUrl = x.MediaUrl,
				MediaType = x.MediaType,
				Post = x.Post,
				PhotoUrl = x.AppUser.PhotoUrl,
				RepliesCount = x.Replies.Count(),
			})
			.ToListAsync();
	}

	public async Task<Comment?> GetById(int postId, int commentId, bool trackChanges)
	{
		if (!trackChanges)
		{
			return await FindByCondition(c => c.Id == commentId && c.PostId == postId, false)
				.Include(c => c.AppUser)
				.Select(x => new Comment
				{
					Id = x.Id,
					AppUserId = x.AppUserId,
					Username = x.AppUser.FullName,
					PostId = x.PostId,
					CommentedAt = x.CommentedAt,
					Content = x.Content,
					MediaUrl = x.MediaUrl,
					MediaType = x.MediaType,
					PhotoUrl = x.AppUser.PhotoUrl,
					RepliesCount = x.Replies.Count(),
				})
				.SingleOrDefaultAsync();
		}

		var comment = await FindByCondition(c => c.Id == commentId && c.PostId == postId, true)
			.Include(c => c.AppUser)
			.Include(c => c.Replies)
			.SingleOrDefaultAsync();

		if (comment is not null)
		{
			comment.Username = comment.AppUser?.FullName;
			comment.PhotoUrl = comment.AppUser?.PhotoUrl;
			comment.RepliesCount = comment.Replies.Count;
		}

		return comment;
	}

	public async Task<IReadOnlyList<CommentReaction>> GetReactionsAsync(IReadOnlyCollection<int> commentIds, bool trackChanges)
	{
		if (commentIds.Count == 0)
		{
			return Array.Empty<CommentReaction>();
		}

		return await _dbContext.CommentReactions
			.AsNoTracking()
			.Where(reaction => commentIds.Contains(reaction.CommentId))
			.ToListAsync();
	}

	public Task<CommentReaction?> GetReactionAsync(int commentId, string userId, bool trackChanges) =>
		(trackChanges ? _dbContext.CommentReactions : _dbContext.CommentReactions.AsNoTracking())
			.FirstOrDefaultAsync(reaction => reaction.CommentId == commentId && reaction.UserId == userId);

	public void AddReaction(CommentReaction reaction) => _dbContext.CommentReactions.Add(reaction);

	public void RemoveReaction(CommentReaction reaction) => _dbContext.CommentReactions.Remove(reaction);
}
