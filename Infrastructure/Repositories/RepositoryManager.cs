﻿using Application.Contracts;
using Domain.Repositories;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositoryManager : IRepositoryManager
{
	private readonly AppDbContext _dbContext;
	private readonly Lazy<IPostRepository> postRepository;
	private readonly Lazy<ICommentRepository> commentRepository;
	private readonly Lazy<IReplyRepository> replyRepository;
	private readonly Lazy<INotificationRepository> notificationRepository;

	public RepositoryManager(AppDbContext dbContext)
	{
		_dbContext = dbContext;
		postRepository = new(() => new PostRepository(dbContext));
		commentRepository = new(() => new CommentRepository(dbContext));
		replyRepository = new(() => new ReplyRepository(dbContext));
		notificationRepository = new(() => new NotificationRepository(dbContext));
	}

	public IPostRepository Posts => postRepository.Value;
	public ICommentRepository Comments => commentRepository.Value;
	public IReplyRepository Replies => replyRepository.Value;
	public INotificationRepository Notifications => notificationRepository.Value;

	public async Task SaveAsync() => await _dbContext.SaveChangesAsync();
}
