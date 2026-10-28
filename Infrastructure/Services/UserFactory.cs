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
				var doctor = mapper.Map<Doctor>(request);
				// Doctor profiles are public by default; account holders can
				// hide them from the profile editor.
				doctor.IsProfilePublic = true;
				user = doctor;
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
