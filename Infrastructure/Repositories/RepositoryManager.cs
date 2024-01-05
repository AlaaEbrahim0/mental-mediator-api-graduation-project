using Infrastructure.Contracts;
using Infrastructure.Data;

namespace Infrastructure.Repositories;

public class RepositoryManager : IRepositoryManager
{
    private readonly AppDbContext _dbContext;
    private readonly Lazy<IPostRepository> postRepository;

    public RepositoryManager(AppDbContext dbContext)
    {
        _dbContext = dbContext;
        postRepository = new Lazy<IPostRepository>(() => new PostRepository(dbContext));
    }
    
    public IPostRepository Posts => postRepository.Value;

    public async Task SaveAsync() => await _dbContext.SaveChangesAsync();
}
