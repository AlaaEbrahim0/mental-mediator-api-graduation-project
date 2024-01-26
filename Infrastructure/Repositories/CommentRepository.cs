﻿using Domain.Entities;
using Infrastructure.Contracts;
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
                Username = $"{x.AppUser!.FirstName} {x.AppUser!.LastName}",
                PostId = x.PostId,
                CommentedAt = x.CommentedAt,
                Content = x.Content,
            })
            .ToListAsync();
    }

    public async Task<Comment?> GetById(int postId, int commentId, bool trackChanges)
    {
        return await FindByCondition(c => c.Id == commentId && c.PostId == postId, trackChanges)
            .Include(c => c.AppUser)
            .Select(x => new Comment
            {
                Id = x.Id,
                AppUserId = x.AppUserId,
                Username = $"{x.AppUser!.FirstName} {x.AppUser!.LastName}",
                PostId = x.PostId,
                CommentedAt = x.CommentedAt,
                Content = x.Content,
            })
            .SingleOrDefaultAsync();
    }

}
