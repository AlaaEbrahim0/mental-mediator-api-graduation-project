using Domain.Entities;
using Shared;

namespace Domain.Repositories;

public interface IUserRepository
{
	Task<IEnumerable<User>> GetAll(UserRequestParameters parameters, bool trackChanges);
	Task<User?> GetById(string id, bool trackChanges);
	void UpdateUserInfo(User user);
	void DeleteUser(User user);
}
