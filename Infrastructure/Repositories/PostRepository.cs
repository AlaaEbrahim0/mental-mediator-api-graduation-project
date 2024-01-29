using Domain.Entities;
using Infrastructure.Contracts;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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

    public async Task<IEnumerable<Post>> GetAllPosts(bool trackChanges)
    {
        return await
            FindAll(trackChanges)
            .Select(p => new Post
            {
                Id = p.Id,
                AppUserId = p.AppUserId,
                Content = p.Content,
                PostedOn = p.PostedOn,
                Title = p.Title,
                Username = p.AppUser!.FullName
            })
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
                Username = p.AppUser!.FullName
            })
            .SingleOrDefaultAsync();
    }

    public void UpdatePost(Post post)
    {
        Update(post);
    }
}


