using Domain.Entities;
using Shared.RequestParameters;

namespace Domain.Repositories;

public interface IUserRepository
{
	Task<IEnumerable<User>> GetAll(UserRequestParameters parameters, bool trackChanges);
	Task<User?> GetById(string id, bool trackChanges);
	Task<int> GetCount();
	void UpdateUserInfo(User user);
	void DeleteUser(User user);
}
