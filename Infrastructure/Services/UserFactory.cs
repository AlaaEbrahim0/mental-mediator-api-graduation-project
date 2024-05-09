using Application.Dtos.AuthDtos;
using AutoMapper;
using Domain.Entities;

namespace Infrastructure.Services;

public class UserFactory
{
	public static BaseUser CreateUser(RegisterationRequest request, IMapper mapper)
	{
		var user = new BaseUser();
		switch (request.Role)
		{
			case "User":
				user = mapper.Map<User>(request);
				break;

			case "Doctor":
				user = mapper.Map<Doctor>(request);
				break;

			default:
				break;
		}
		return user;
	}
}