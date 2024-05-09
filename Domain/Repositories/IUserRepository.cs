using Domain.Entities;

namespace Domain.Repositories;

public interface IUserRepository
{
	Task<User?> GetById(string id, bool trackChanges);
	void UpdateUserInfo(User user);
}
