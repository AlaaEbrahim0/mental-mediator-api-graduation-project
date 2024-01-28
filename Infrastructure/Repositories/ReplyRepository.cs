using Domain.Entities;
using Infrastructure.Contracts;
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
                Username = r.AppUser!.UserName
            })
            .ToListAsync();

        return replies;
    }

    public async Task<Reply?> GetById(int postId, int commentId, int replyId, bool trackChanges)
    {
        return await
            FindByCondition(
                r => r.Id == replyId &&
                r.CommentId == commentId &&
                r.Comment!.PostId == postId,
                trackChanges)
            .Include(r => r.CommentId)
            .Include(r => r.AppUser)
            .Select(r => new Reply
            {
                Id = r.Id,
                AppUserId = r.AppUserId,
                CommentId = r.CommentId,
                Content = r.Content,
                RepliedAt = r.RepliedAt,
                Username = r.AppUser!.UserName
            })
            .SingleOrDefaultAsync();

    }

    public void UpdateReply(Reply reply)
    {
        Update(reply);
    }
}
