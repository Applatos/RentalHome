using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Sommerhus.Core.Dtos.Security;

namespace Sommerhus.Core.Identity;

public sealed class AdminIdentitySeeder(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<DefaultAdminOptions> options,
    ILogger<AdminIdentitySeeder> logger)
{
    private readonly UserManager<ApplicationUser> userManager = userManager;
    private readonly RoleManager<IdentityRole> roleManager = roleManager;
    private readonly DefaultAdminOptions adminOptions = options.Value;
    private readonly ILogger<AdminIdentitySeeder> logger = logger;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Ensure all application roles exist
        foreach (var roleName in AppRoles.All)
        {
            await EnsureRoleAsync(roleName);
        }

        if (string.IsNullOrWhiteSpace(adminOptions.UserName) || string.IsNullOrWhiteSpace(adminOptions.Password))
        {
            logger.LogWarning("Default admin credentials are not configured. Skipping admin seeding.");
            return;
        }

        var user = await userManager.FindByNameAsync(adminOptions.UserName);
        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = adminOptions.UserName,
                Email = adminOptions.Email,
                EmailConfirmed = true
            };

            var createResult = await userManager.CreateAsync(user, adminOptions.Password);
            if (!createResult.Succeeded)
            {
                LogErrors("Failed to create default admin user", createResult.Errors);
                return;
            }

            logger.LogInformation("Created default admin user {UserName}.", adminOptions.UserName);
        }

        if (!await userManager.IsInRoleAsync(user, AppRoles.Admin))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, AppRoles.Admin);
            if (!addRoleResult.Succeeded)
            {
                LogErrors("Failed to add default admin user to Admin role", addRoleResult.Errors);
            }
        }
    }

    private async Task EnsureRoleAsync(string roleName)
    {
        var role = await roleManager.FindByNameAsync(roleName);
        if (role is not null) return;

        var result = await roleManager.CreateAsync(new IdentityRole(roleName));
        if (!result.Succeeded)
        {
            LogErrors($"Failed to create {roleName} role", result.Errors);
            throw new InvalidOperationException($"Unable to create {roleName} role for default seeding.");
        }

        logger.LogInformation("Created {Role} role.", roleName);
    }

    private void LogErrors(string message, IEnumerable<IdentityError> errors)
    {
        var descriptions = string.Join(", ", errors.Select(e => $"{e.Code}: {e.Description}"));
        logger.LogError("{Message}: {Errors}", message, descriptions);
    }
}
