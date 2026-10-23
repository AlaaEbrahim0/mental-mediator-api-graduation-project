using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

public static class DbInitializer
{
	public static async Task InitializeDatabaseAsync(this IApplicationBuilder app)
	{
		using var serviceScope = app.ApplicationServices.CreateScope();
		var services = serviceScope.ServiceProvider;
		var context = services.GetRequiredService<AppDbContext>();
		var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");

		// Apply pending migrations only when the database actually exists. A
		// missing or unreachable database must not be created implicitly from
		// the application: on a shared host a typoed connection string would
		// otherwise attempt to materialize a database at every start.
		// When the database exists but is empty, migrations build the whole
		// schema additively; data lives in scripts and is seeded with
		// scripts/seed.sql, not from application startup.
		if (!await context.Database.CanConnectAsync())
		{
			logger.LogWarning(
				"Database is not reachable; skipping migrations. Check ConnectionStrings:constr_somee, then run scripts/seed.sql after the schema is migrated.");
			return;
		}

		var pending = await context.Database.GetPendingMigrationsAsync();
		var pendingList = pending.ToList();
		if (pendingList.Count == 0)
		{
			logger.LogInformation("Database schema is up to date.");
			return;
		}

		logger.LogInformation(
			"Applying {Count} pending migration(s): {Migrations}",
			pendingList.Count,
			string.Join(", ", pendingList));

		await context.Database.MigrateAsync();
		logger.LogInformation("Database migrations applied successfully.");
	}
}
