using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
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
        return await FindAll(trackChanges).ToListAsync();
    }

    public async Task<Post?> GetPostById(int id, bool trackChanges)
    {
        return await FindByCondition(p => p.Id == id, trackChanges)
                    .SingleOrDefaultAsync();
    }

    public void UpdatePost(Post post)
    {
        Update(post);
    }
}


