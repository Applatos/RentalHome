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

        if (string.IsNullOrWhiteSpace(adminOptions.UserName) || string.IsNullOrWhiteSpace(adminOptions.Password))
        {
            logger.LogWarning("Default admin credentials are not configured. Skipping admin seeding.");
            return;
        }

        var adminRole = await EnsureAdminRoleAsync();
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

        if (!await userManager.IsInRoleAsync(user, adminRole.Name!))
        {
            var addRoleResult = await userManager.AddToRoleAsync(user, adminRole.Name!);
            if (!addRoleResult.Succeeded)
            {
                LogErrors("Failed to add default admin user to Admin role", addRoleResult.Errors);
            }
        }
    }

    private async Task<IdentityRole> EnsureAdminRoleAsync()
    {
        var role = await roleManager.FindByNameAsync(AdminRoles.Admin);
        if (role is not null)
        {
            return role;
        }

        role = new IdentityRole(AdminRoles.Admin);
        var result = await roleManager.CreateAsync(role);
        if (!result.Succeeded)
        {
            LogErrors("Failed to create Admin role", result.Errors);
            throw new InvalidOperationException("Unable to create Admin role for default seeding.");
        }

        logger.LogInformation("Created {Role} role for admin access.", AdminRoles.Admin);
        return role;
    }

    private void LogErrors(string message, IEnumerable<IdentityError> errors)
    {
        var descriptions = string.Join(", ", errors.Select(e => $"{e.Code}: {e.Description}"));
        logger.LogError("{Message}: {Errors}", message, descriptions);
    }
}
