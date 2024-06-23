using Bogus;
using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Data;
public static class DbInitializer
{
	public static void InitializeDatabase(this IApplicationBuilder app)
	{

		using (var serviceScope = app.ApplicationServices.CreateScope())
		{
			var context = serviceScope.ServiceProvider.GetService<AppDbContext>()!;
			SeedData(context);
		}

	}

	private static void SeedData(AppDbContext context)
	{
		//if (isProductionEnv)
		//{
		//    Console.WriteLine("Applying pending migrations");
		//    context.Database.Migrate();
		//}

		if (context.Roles.Any())
		{
			Console.WriteLine("already seeded");
			return;
		}

		var userFaker = new Faker<BaseUser>()
			.RuleFor(u => u.Id, set => Guid.NewGuid().ToString())
			.RuleFor(u => u.FirstName, set => set.Person.FirstName)
			.RuleFor(u => u.LastName, set => set.Person.LastName)
			.RuleFor(u => u.BirthDate, set => set.Date.BetweenDateOnly(new DateOnly(1950, 1, 1), new DateOnly(2003, 1, 1)))
			.RuleFor(u => u.Email, set => set.Person.Email)
			.RuleFor(u => u.UserName, set => set.Person.UserName)
			.RuleFor(u => u.NormalizedUserName, set => set.Person.UserName.ToUpper())
			.RuleFor(u => u.NormalizedEmail, set => set.Person.Email.ToUpper())
			.RuleFor(u => u.Gender, set => set.PickRandom("male", "female"))
			.RuleFor(u => u.PasswordHash, set => set.Internet.Password(10))
			.RuleFor(u => u.EmailConfirmed, set => true);


		var users = userFaker.Generate(25);
		var doctorFaker = new Faker<Doctor>()
			.RuleFor(x => x.Biography, set => set.Lorem.Sentences(set.PickRandom(1, 2, 3)))
			.RuleFor(x => x.SessionFees, set => set.Random.Number(100, 2000))
			.RuleFor(x => x.Location, set => set.Address.FullAddress())
			.RuleFor(x => x.Specialization, set => set.PickRandomParam(Enum.GetValues<DoctorSpecialization>())); ;

		for (int i = 0; i < users.Count; ++i)
		{
			doctorFaker.RuleFor(x => x.Id, setter => users[i].Id);
		}
		var doctor = doctorFaker.Generate(25);


	}
}
