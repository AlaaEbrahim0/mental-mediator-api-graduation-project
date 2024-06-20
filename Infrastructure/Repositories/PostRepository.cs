﻿using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared;

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

	public async Task<IEnumerable<Post>> GetAllPosts(PostRequestParameters parameters, bool trackChanges)
	{
		return await
			FindByCondition(x => !x.IsAnonymous, trackChanges)
			.OrderByDescending(c => c.PostedOn)
			.Select(p => new Post
			{
				Id = p.Id,
				AppUserId = p.AppUserId,
				Content = p.Content,
				PostedOn = p.PostedOn,
				Title = p.Title,
				IsAnonymous = p.IsAnonymous,
				Username = p.IsAnonymous ? null : p.AppUser!.FullName,
				PhotoUrl = p.IsAnonymous ? null : p.AppUser!.PhotoUrl,
				PostPhotoUrl = p.PostPhotoUrl,
				CommentsCount = p.Comments.Count()
			})
			.Paginate(parameters.PageNumber, parameters.PageSize)
			.ToListAsync();

	}

	public async Task<IEnumerable<Post>> GetConfessionOnly(PostRequestParameters parameters, bool trackChanges)
	{
		return await
			FindByCondition(x => x.IsAnonymous, trackChanges)
			.OrderByDescending(c => c.PostedOn)
			.Select(p => new Post
			{
				Id = p.Id,
				AppUserId = p.AppUserId,
				Content = p.Content,
				PostedOn = p.PostedOn,
				Title = p.Title,
				IsAnonymous = p.IsAnonymous,
				Username = p.IsAnonymous ? null : p.AppUser!.FullName,
				PhotoUrl = p.IsAnonymous ? null : p.AppUser!.PhotoUrl,
				PostPhotoUrl = p.PostPhotoUrl,
				CommentsCount = p.Comments.Count()
			})
			.Paginate(parameters.PageNumber, parameters.PageSize)
			.ToListAsync();
	}

	public async Task<IEnumerable<Post>> GetPostsByUserId(string userId, PostRequestParameters
		parameters, bool trackChanges)
	{
		return await
			FindByCondition(p => p.AppUserId == userId, trackChanges)
			.OrderByDescending(c => c.PostedOn)
			.Select(p => new Post
			{
				Id = p.Id,
				AppUserId = p.AppUserId,
				Content = p.Content,
				PostedOn = p.PostedOn,
				Title = p.Title,
				IsAnonymous = p.IsAnonymous,
				Username = p.IsAnonymous ? null : p.AppUser!.FullName,
				PhotoUrl = p.IsAnonymous ? null : p.AppUser!.PhotoUrl,
				PostPhotoUrl = p.PostPhotoUrl,
				CommentsCount = p.Comments.Count()

			})
			.Paginate(parameters.PageNumber, parameters.PageSize)
			.ToListAsync();
	}

	public async Task<Post?> GetPostById(int id, bool trackChanges)
	{

		return await
			FindByCondition(p => p.Id == id, trackChanges)
			.Include(p => p.AppUser)
			.Select(p => new Post
			{
				Id = p.Id,
				AppUserId = p.AppUserId,
				Content = p.Content,
				PostedOn = p.PostedOn,
				Title = p.Title,
				Username = p.IsAnonymous ? null : p.AppUser!.FullName,
				PhotoUrl = p.IsAnonymous ? null : p.AppUser!.PhotoUrl,
				IsAnonymous = p.IsAnonymous,
				PostPhotoUrl = p.PostPhotoUrl,
				CommentsCount = p.Comments.Count()

			})
			.SingleOrDefaultAsync();
	}

	public void UpdatePost(Post post)
	{
		Update(post);
	}
}
