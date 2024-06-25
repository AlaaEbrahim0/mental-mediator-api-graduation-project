using Domain.Entities;
using Domain.Repositories;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared;

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

	public void DeleteUser(User user)
	{
		Delete(user);
	}

	public async Task<IEnumerable<User>> GetAll(UserRequestParameters requestParameters, bool trackChanges)
	{
		var userQuery = FindAll(trackChanges);

		if (!string.IsNullOrWhiteSpace(requestParameters.Name))
		{
			userQuery = userQuery.Where(d =>
				d.FirstName.Contains(requestParameters.Name) ||
				d.LastName.Contains(requestParameters.Name));
		}

		if (!string.IsNullOrWhiteSpace(requestParameters.Gender))
		{
			userQuery = userQuery.Where(d => d.Gender == requestParameters.Gender);
		}

		var users = await userQuery
			.Paginate(requestParameters.PageNumber, requestParameters.PageSize)
			.ToListAsync();

		return users;
	}

}
