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
			.Select(x => new Comment
			{
				Id = x.Id,
				AppUserId = x.AppUserId,
				Username = x.AppUser!.FullName,
				PostId = x.PostId,
				CommentedAt = x.CommentedAt,
				Content = x.Content,
				PhotoUrl = x.AppUser.PhotoUrl,
				RepliesCount = x.Replies.Count(),
			})
			.ToListAsync();
	}

	public async Task<Comment?> GetById(int postId, int commentId, bool trackChanges)
	{
		return await FindByCondition(c => c.Id == commentId && c.PostId == postId, trackChanges)
			.Include(c => c.AppUser)
			.Include(c => c.Replies)
			.Select(x => new Comment
			{
				Id = x.Id,
				AppUserId = x.AppUserId,
				Username = x.AppUser!.FullName,
				PostId = x.PostId,
				CommentedAt = x.CommentedAt,
				Content = x.Content,
				PhotoUrl = x.AppUser.PhotoUrl,
				RepliesCount = x.Replies.Count(),

			})
			.SingleOrDefaultAsync();
	}
}
