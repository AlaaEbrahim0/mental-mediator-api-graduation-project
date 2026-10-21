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

		await SeedBootstrapAdminAsync(configuration, context, userManager, logger);
		await SeedBootstrapDoctorAsync(configuration, context, userManager, logger);
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

	private static async Task SeedBootstrapDoctorAsync(
		IConfiguration configuration,
		AppDbContext context,
		UserManager<BaseUser> userManager,
		ILogger logger)
	{
		var email = configuration["DoctorSeed:Email"]?.Trim();
		var password = configuration["DoctorSeed:Password"];
		var hasEmail = !string.IsNullOrWhiteSpace(email);
		var hasPassword = !string.IsNullOrWhiteSpace(password);

		if (!hasEmail && !hasPassword) return;
		if (!hasEmail || !hasPassword)
		{
			throw new InvalidOperationException(
				"Set both DoctorSeed__Email and DoctorSeed__Password to seed the bootstrap doctor.");
		}

		var existingUser = await userManager.FindByEmailAsync(email!);
		if (existingUser is not null)
		{
			var isDoctorEntity = await context.Doctors.AnyAsync(doctor => doctor.Id == existingUser.Id);
			if (!isDoctorEntity)
			{
				throw new InvalidOperationException(
					$"DoctorSeed email '{email}' already belongs to a non-doctor account. Choose an unused email.");
			}

			await EnsureDoctorRoleAsync(userManager, existingUser, email!);
			if (!existingUser.EmailConfirmed)
			{
				existingUser.EmailConfirmed = true;
				var updateResult = await userManager.UpdateAsync(existingUser);
				EnsureSucceeded(updateResult, "confirm the bootstrap doctor's email");
			}
			return;
		}

		var birthDateText = configuration["DoctorSeed:BirthDate"] ?? "1990-01-01";
		if (!DateOnly.TryParse(birthDateText, out var birthDate) || birthDate >= DateOnly.FromDateTime(DateTime.UtcNow))
		{
			throw new InvalidOperationException("DoctorSeed__BirthDate must be a valid past date in YYYY-MM-DD format.");
		}

		var gender = configuration["DoctorSeed:Gender"] ?? "female";
		if (!gender.Equals("male", StringComparison.OrdinalIgnoreCase) &&
			!gender.Equals("female", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("DoctorSeed__Gender must be either male or female.");
		}

		var specializationText = configuration["DoctorSeed:Specialization"] ?? nameof(DoctorSpecialization.ClinicalPsychology);
		if (!Enum.TryParse<DoctorSpecialization>(specializationText, true, out var specialization))
		{
			throw new InvalidOperationException($"DoctorSeed__Specialization must be a valid {nameof(DoctorSpecialization)} value.");
		}

		var doctor = new Doctor
		{
			Email = email,
			EmailConfirmed = true,
			FirstName = configuration["DoctorSeed:FirstName"] ?? "Local",
			LastName = configuration["DoctorSeed:LastName"] ?? "Doctor",
			BirthDate = birthDate,
			Gender = gender.ToLowerInvariant(),
			City = configuration["DoctorSeed:City"] ?? "Cairo",
			Specialization = specialization,
			IsAvailable = true,
		};

		var createResult = await userManager.CreateAsync(doctor, password!);
		EnsureSucceeded(createResult, "create the bootstrap doctor (check email and password policy)");
		await EnsureDoctorRoleAsync(userManager, doctor, email!);
		logger.LogInformation("Seeded the configured bootstrap doctor {Email}.", email);
	}

	private static async Task EnsureDoctorRoleAsync(UserManager<BaseUser> userManager, BaseUser user, string email)
	{
		if (await userManager.IsInRoleAsync(user, "Doctor")) return;

		var result = await userManager.AddToRoleAsync(user, "Doctor");
		EnsureSucceeded(result, $"assign the Doctor role to '{email}'");
	}

	private static async Task SeedBootstrapAdminAsync(
		IConfiguration configuration,
		AppDbContext context,
		UserManager<BaseUser> userManager,
		ILogger logger)
	{
		var email = configuration["AdminSeed:Email"]?.Trim();
		var password = configuration["AdminSeed:Password"];
		var hasEmail = !string.IsNullOrWhiteSpace(email);
		var hasPassword = !string.IsNullOrWhiteSpace(password);

		if (!hasEmail && !hasPassword)
		{
			return;
		}

		if (!hasEmail || !hasPassword)
		{
			throw new InvalidOperationException(
				"Set both AdminSeed__Email and AdminSeed__Password to seed the bootstrap administrator.");
		}

		var existingUser = await userManager.FindByEmailAsync(email!);
		if (existingUser is not null)
		{
			var isAdminEntity = await context.Admins.AnyAsync(admin => admin.Id == existingUser.Id);
			if (!isAdminEntity)
			{
				throw new InvalidOperationException(
					$"AdminSeed email '{email}' already belongs to a non-admin account. Choose an unused email.");
			}

			await EnsureAdminRoleAsync(userManager, existingUser, email!);
			if (!existingUser.EmailConfirmed)
			{
				existingUser.EmailConfirmed = true;
				var updateResult = await userManager.UpdateAsync(existingUser);
				EnsureSucceeded(updateResult, "confirm the bootstrap administrator email");
			}

			return;
		}

		var birthDateText = configuration["AdminSeed:BirthDate"] ?? "1990-01-01";
		if (!DateOnly.TryParse(birthDateText, out var birthDate) || birthDate >= DateOnly.FromDateTime(DateTime.UtcNow))
		{
			throw new InvalidOperationException("AdminSeed__BirthDate must be a valid past date in YYYY-MM-DD format.");
		}

		var gender = configuration["AdminSeed:Gender"] ?? "male";
		if (!gender.Equals("male", StringComparison.OrdinalIgnoreCase) &&
			!gender.Equals("female", StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidOperationException("AdminSeed__Gender must be either male or female.");
		}

		var admin = new Admin
		{
			Email = email,
			EmailConfirmed = true,
			FirstName = configuration["AdminSeed:FirstName"] ?? "Local",
			LastName = configuration["AdminSeed:LastName"] ?? "Admin",
			BirthDate = birthDate,
			Gender = gender.ToLowerInvariant(),
		};

		var createResult = await userManager.CreateAsync(admin, password!);
		EnsureSucceeded(createResult, "create the bootstrap administrator (check email and password policy)");

		await EnsureAdminRoleAsync(userManager, admin, email!);
		logger.LogInformation("Seeded the configured bootstrap administrator {Email}.", email);
	}

	private static async Task EnsureAdminRoleAsync(UserManager<BaseUser> userManager, BaseUser user, string email)
	{
		if (await userManager.IsInRoleAsync(user, "Admin"))
		{
			return;
		}

		var result = await userManager.AddToRoleAsync(user, "Admin");
		EnsureSucceeded(result, $"assign the Admin role to '{email}'");
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
