using Domain.Entities;
using Domain.Enums;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

public static class DbInitializer
{
	private static readonly string[] Roles = ["User", "Doctor", "Admin"];

	public static async Task InitializeDatabaseAsync(this IApplicationBuilder app)
	{
		using var serviceScope = app.ApplicationServices.CreateScope();
		var services = serviceScope.ServiceProvider;
		var context = services.GetRequiredService<AppDbContext>();
		var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
		var userManager = services.GetRequiredService<UserManager<BaseUser>>();
		var configuration = services.GetRequiredService<IConfiguration>();
		var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

		// Apply pending migrations before seeding. Without this the new tables
		// (for example PostLikes) never reach the hosted database, because the
		// deploy pipeline builds and publishes the image but never runs
		// `dotnet ef database update`.
		await ApplyPendingMigrationsAsync(context, logger);

		await using var transaction = await context.Database.BeginTransactionAsync();

		foreach (var roleName in Roles)
		{
			if (await roleManager.RoleExistsAsync(roleName))
			{
				continue;
			}

			var result = await roleManager.CreateAsync(new IdentityRole(roleName));
			EnsureSucceeded(result, $"create the {roleName} role");
			logger.LogInformation("Created missing identity role {RoleName}.", roleName);
		}

		await transaction.CommitAsync();
	}

	private static async Task ApplyPendingMigrationsAsync(AppDbContext context, ILogger logger)
	{
		try
		{
			var pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
			if (pending.Count == 0)
			{
				logger.LogInformation("Database schema is up to date.");
				return;
			}

			logger.LogInformation(
				"Applying {Count} pending migration(s): {Migrations}",
				pending.Count,
				string.Join(", ", pending));

			await context.Database.MigrateAsync();
			logger.LogInformation("Database migrations applied successfully.");
		}
		catch (Exception exception)
		{
			// A failed migration must not take the whole API down: the database may
			// be temporarily unreachable. Log it and let the app start so the
			// health endpoint stays available and a retry can happen on the next
			// deployment.
			logger.LogError(exception, "Failed to apply database migrations.");
		}
	}

	private static void EnsureSucceeded(IdentityResult result, string operation)
	{
		if (result.Succeeded)
		{
			return;
		}

		var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
		throw new InvalidOperationException($"Could not {operation}: {errors}");
	}
}
