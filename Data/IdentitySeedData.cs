using CampusEvents.Models;
using CampusEvents.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace CampusEvents.Data;

public static class IdentitySeedData
{
	private const string ExistingOrganizerId = "seed-organizer-001";
	private const string OrganizerDisplayName = "Campus Events Office";
	private const string AdministratorDisplayName = "CEMS Administrator";

	public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
	{
		var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
		var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

		var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeedData");

		await EnsureRoleAsync(roleManager, RoleNames.Administrator);

		var administratorEmail = configuration["SeedAccounts:Administrator:Email"];
		var administratorPassword = configuration["SeedAccounts:Administrator:Password"];
		var organizerEmail = configuration["SeedAccounts:Organizer:Email"];
		var organizerPassword = configuration["SeedAccounts:Organizer:Password"];

		var canSeedClassroomAccounts =
			!string.IsNullOrWhiteSpace(administratorEmail) &&
			!string.IsNullOrWhiteSpace(administratorPassword) &&
			!string.IsNullOrWhiteSpace(organizerEmail) &&
			!string.IsNullOrWhiteSpace(organizerPassword);

		if (canSeedClassroomAccounts)
		{
			await EnsureAdministratorAsync(
				userManager,
				administratorEmail!,
				administratorPassword!);
		}

		await EnsureRoleAsync(roleManager, RoleNames.Organizer);

		if (!canSeedClassroomAccounts)
		{
			logger.LogWarning(
				"Development roles exist, but accounts were not " +
				"seeded because one or more SeedAccounts user-secret values " +
				"are missing.");

			return;
		}

		await EnsureOrganizerAsync(userManager, organizerEmail!, organizerPassword!);
	}

	private static void EnsureSucceeded(IdentityResult result, string operation)
	{
		if (result.Succeeded)
		{
			return;
		}

		var descriptions = string.Join("; ", result.Errors.Select(error => error.Description));

		throw new InvalidOperationException($"Identity seeding failed while {operation}: {descriptions}");
	}

	private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
	{
		if (await roleManager.RoleExistsAsync(roleName))
		{
			return;
		}

		var result = await roleManager.CreateAsync(new IdentityRole(roleName));

		EnsureSucceeded(result, $"creating role '{roleName}'");
	}

	private static async Task EnsureAdministratorAsync(UserManager<ApplicationUser> userManager, string email, string password)
	{
		var user = await userManager.FindByEmailAsync(email);

		if (user is null)
		{
			user = new ApplicationUser
			{
				UserName = email,
				Email = email,
				DisplayName = AdministratorDisplayName
			};

			var createResult = await userManager.CreateAsync(user, password);

			EnsureSucceeded(createResult, "creating the classroom Administrator account");
		}
		else
		{
			user.DisplayName = AdministratorDisplayName;

			var updateResult = await userManager.UpdateAsync(user);

			EnsureSucceeded(updateResult, "updating the classroom Administrator account");

			if (!await userManager.HasPasswordAsync(user))
			{
				var passwordResult = await userManager.AddPasswordAsync(user, password);

				EnsureSucceeded(passwordResult, "adding the classroom Administrator password");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Administrator))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Administrator);

			EnsureSucceeded(roleResult, "assigning the Administrator role");
		}
	}

	private static async Task EnsureOrganizerAsync(UserManager<ApplicationUser> userManager, string email, string password)
	{
		var user = await userManager.FindByIdAsync(ExistingOrganizerId);

		if (user is null)
		{
			user = new ApplicationUser
			{
				Id = ExistingOrganizerId,
				UserName = email,
				Email = email,
				DisplayName = OrganizerDisplayName
			};

			var createResult = await userManager.CreateAsync(user, password);

			EnsureSucceeded(createResult, "creating the classroom Organizer account");
		}
		else
		{
			if (string.IsNullOrEmpty(user.SecurityStamp))
			{
				user.SecurityStamp = Guid.NewGuid().ToString();
			}

			user.UserName = email;
			user.Email = email;
			user.DisplayName = OrganizerDisplayName;

			var updateResult = await userManager.UpdateAsync(user);

			EnsureSucceeded(updateResult, "updating the existing seed Organizer identity");

			if (!await userManager.HasPasswordAsync(user))
			{
				var passwordResult = await userManager.AddPasswordAsync(user, password);

				EnsureSucceeded(passwordResult, "adding the classroom Organizer password");
			}
		}

		if (!await userManager.IsInRoleAsync(user, RoleNames.Organizer))
		{
			var roleResult = await userManager.AddToRoleAsync(user, RoleNames.Organizer);

			EnsureSucceeded(roleResult, "assigning the Organizer role");
		}
	}
}
