using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;
public class ReplyRepository : RepositoryBase<Reply>, IReplyRepository
{
	public ReplyRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public void CreateReply(Reply reply)
	{
		Create(reply);
	}

	public void DeleteReply(Reply reply)
	{
		Delete(reply);
	}

	public async Task<IEnumerable<Reply?>> GetRepliesByCommentId(int commendId, bool trackChanges)
	{
		var replies = await FindByCondition(c => c.CommentId == commendId, trackChanges)
			.Include(r => r.AppUser)
			.Select(r => new Reply
			{
				Id = r.Id,
				AppUserId = r.AppUserId,
				CommentId = r.CommentId,
				Content = r.Content,
				RepliedAt = r.RepliedAt,
				Username = r.AppUser!.FullName,
				PhotoUrl = r.AppUser!.PhotoUrl,
			})
			.ToListAsync();

		return replies;
	}

	public async Task<Reply?> GetById(int postId, int commentId, int replyId, bool trackChanges)
	{
		var query = FindByCondition(
			r => r.Id == replyId &&
				r.CommentId == commentId &&
				r.Comment!.PostId == postId,
			trackChanges);

		if (!trackChanges)
		{
			return await query
				.Include(r => r.Comment)
				.Include(r => r.AppUser)
				.Select(r => new Reply
				{
					Id = r.Id,
					AppUserId = r.AppUserId,
					CommentId = r.CommentId,
					Content = r.Content,
					RepliedAt = r.RepliedAt,
					PhotoUrl = r.AppUser!.PhotoUrl,
					Username = r.AppUser!.FullName
				})
				.SingleOrDefaultAsync();
		}

		var reply = await query
			.Include(r => r.Comment)
			.Include(r => r.AppUser)
			.SingleOrDefaultAsync();

		if (reply is not null)
		{
			reply.Username = reply.AppUser?.FullName ?? string.Empty;
			reply.PhotoUrl = reply.AppUser?.PhotoUrl;
		}

		return reply;
	}

	public void UpdateReply(Reply reply)
	{
		Update(reply);
	}
}
