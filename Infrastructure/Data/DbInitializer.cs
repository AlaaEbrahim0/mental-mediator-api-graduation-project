using Bogus;
using Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
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

		if (context.Posts.Any() ||
			context.Users.Any() ||
			context.UserRoles.Any())
		{
			Console.WriteLine("already seeded");
			return;
		}

		var rolesIds = Enumerable.Range(1, 3).Select(x => Guid.NewGuid().ToString()).ToArray();
		var roles = new List<IdentityRole>()
		{
			new IdentityRole() { Id = rolesIds[0], Name = "User", NormalizedName = "USER"},

			new IdentityRole() { Id = rolesIds[1], Name = "Admin", NormalizedName = "ADMIN"},

			new IdentityRole() { Id = rolesIds[2], Name = "Doctor", NormalizedName = "DOCTOR"},
		};

		context.Roles.AddRange(roles);

		var userIds = Enumerable
			.Range(1, 25)
			.Select(x => Guid.NewGuid().ToString())
			.ToArray();

		int id = 0;



		var userFaker = new Faker<AppUser>()
			.RuleFor(u => u.Id, set => userIds[id++])
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

		context.AddRange(users);

		id = 0;

		foreach (var user in users)
		{
			context.UserRoles.Add(new IdentityUserRole<string>()
			{ UserId = user.Id, RoleId = rolesIds[0] });
		}

		var postFaker = new Faker<Post>();

		var faker = new Faker();

		string postTitle = faker.Lorem.Text();
		string postBody = faker.Lorem.Sentences(faker.PickRandom(1, 2, 3, 4, 5, 6, 7, 8, 9, 10));

		postTitle = postTitle.Substring(0, Math.Min(postTitle.Length, 255));
		postBody = postBody.Substring(0, Math.Min(postBody.Length, 2047));

		postFaker
		.RuleFor(x => x.PostedOn, set => set.Date.Past())
		.RuleFor(x => x.Title, postTitle)
		.RuleFor(x => x.Content, postBody)
		.RuleFor(x => x.AppUserId, set => set.Random.ArrayElement(userIds));

		var posts = postFaker.Generate(100);
		context.AddRange(posts);
		var postIds = Enumerable.Range(1, 100).ToArray();

		context.SaveChanges();

		var commentFaker = new Faker<Comment>()
			.RuleFor(c => c.AppUserId, set => set.Random.ArrayElement(userIds))
			.RuleFor(c => c.CommentedAt, set => set.Date.Past())
			.RuleFor(c => c.Content, set => set.Lorem.Sentences(set.PickRandom(1, 2, 3)))
			.RuleFor(c => c.PostId, set => set.Random.ArrayElement(postIds));

		var comments = commentFaker.Generate(250);
		context.AddRange(comments);
		var commentIds = Enumerable.Range(1, 250).ToArray();

		context.SaveChanges();

		var replyFaker = new Faker<Reply>()
			.RuleFor(c => c.AppUserId, set => set.Random.ArrayElement(userIds))
			.RuleFor(c => c.RepliedAt, set => set.Date.Past())
			.RuleFor(c => c.Content, set => set.Lorem.Sentences(set.PickRandom(1, 2, 3)))
			.RuleFor(c => c.CommentId, set => set.Random.ArrayElement(commentIds));

		var replies = replyFaker.Generate(500);

		context.Replies.AddRange(replies);

		context.SaveChanges();

		Console.WriteLine("database was seeded successfully");
	}
}
