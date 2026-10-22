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

		// Somee databases can contain a migration-history row without all of the
		// objects from that migration (for example after an interrupted FTP/app
		// restart). Reconcile the post schema before EF starts serving requests.
		await EnsurePostSchemaAsync(context, logger);

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

	private static async Task EnsurePostSchemaAsync(AppDbContext context, ILogger logger)
	{
		try
		{
			await context.Database.ExecuteSqlRawAsync("""
				IF COL_LENGTH('Posts', 'Category') IS NULL
					ALTER TABLE [Posts] ADD [Category] nvarchar(100) NULL;

				IF COL_LENGTH('Posts', 'Tags') IS NULL
					ALTER TABLE [Posts] ADD [Tags] nvarchar(1000) NULL;

				IF OBJECT_ID(N'[PostLikes]', N'U') IS NULL
				BEGIN
					CREATE TABLE [PostLikes] (
						[Id] int NOT NULL IDENTITY,
						[PostId] int NOT NULL,
						[UserId] nvarchar(450) NOT NULL,
						[LikedOn] datetime2 NOT NULL,
						CONSTRAINT [PK_PostLikes] PRIMARY KEY CLUSTERED ([Id])
					);
				END
				""");

			await context.Database.ExecuteSqlRawAsync("""
				IF OBJECT_ID(N'[PostLikes]', N'U') IS NOT NULL
				   AND OBJECT_ID(N'[FK_PostLikes_Posts_PostId]', N'F') IS NULL
				BEGIN
					ALTER TABLE [PostLikes] WITH NOCHECK ADD
						CONSTRAINT [FK_PostLikes_Posts_PostId] FOREIGN KEY ([PostId])
						REFERENCES [Posts] ([Id]) ON DELETE CASCADE;
				END

				IF OBJECT_ID(N'[PostLikes]', N'U') IS NOT NULL
				   AND OBJECT_ID(N'[FK_PostLikes_AspNetUsers_UserId]', N'F') IS NULL
				BEGIN
					ALTER TABLE [PostLikes] WITH NOCHECK ADD
						CONSTRAINT [FK_PostLikes_AspNetUsers_UserId] FOREIGN KEY ([UserId])
						REFERENCES [AspNetUsers] ([Id]);
				END

				IF NOT EXISTS (
					SELECT 1 FROM sys.indexes
					WHERE name = N'IX_PostLikes_UserId_PostId'
					  AND object_id = OBJECT_ID(N'[PostLikes]'))
				BEGIN
					CREATE UNIQUE INDEX [IX_PostLikes_UserId_PostId]
						ON [PostLikes] ([UserId], [PostId]);
				END
				""");

			logger.LogInformation("Post schema reconciliation completed.");
		}
		catch (Exception exception)
		{
			// The regular migration path below remains authoritative. This repair
			// is best-effort because a transient database outage must not prevent
			// the health endpoint from starting.
			logger.LogWarning(exception, "Post schema reconciliation could not be completed.");
		}
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
