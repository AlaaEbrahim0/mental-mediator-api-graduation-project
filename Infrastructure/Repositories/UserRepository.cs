using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class UserRepository : RepositoryBase<User>, IUserRepository
{
	public UserRepository(AppDbContext dbContext) : base(dbContext)
	{
	}

	public async Task<User?> GetById(string id, bool trackChanges)
	{
		var user = await FindByCondition(d => d.Id == id, trackChanges).FirstOrDefaultAsync();
		return user;
	}

	public void UpdateUserInfo(User user)
	{
		Update(user);
	}
}
