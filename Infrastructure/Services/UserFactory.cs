using Application.Dtos.AuthDtos;
using AutoMapper;
using Domain.Entities;

namespace Infrastructure.Services;

public class UserFactory
{
	public static BaseUser CreateUser(RegisterationRequest request, IMapper mapper)
	{
		var user = new BaseUser();
		switch (request.Role.ToUpperInvariant())
		{
			case "USER":
				user = mapper.Map<User>(request);
				break;

			case "DOCTOR":
				user = mapper.Map<Doctor>(request);
				break;

			case "ADMIN":
				user = mapper.Map<Admin>(request);
				break;

			default:
				break;
		}
		return user;
	}
}
