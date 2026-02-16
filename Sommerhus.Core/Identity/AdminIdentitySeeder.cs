using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


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

        // Seed a test Owner account
        await EnsureUserAsync(
            userName: "owner",
            email: "owner@sommerhus.dk",
            password: "Owner123!",
            role: AppRoles.HouseOwner,
            firstName: "Ole",
            lastName: "Jensen");

        // Seed a test User account
        await EnsureUserAsync(
            userName: "user",
            email: "user@sommerhus.dk",
            password: "User123!",
            role: AppRoles.User,
            firstName: "Karen",
            lastName: "Nielsen");
    }

    private async Task EnsureUserAsync(
        string userName,
        string email,
        string password,
        string role,
        string? firstName = null,
        string? lastName = null)
    {
        var existing = await userManager.FindByNameAsync(userName);
        if (existing is not null) return;

        var newUser = new ApplicationUser
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            FirstName = firstName,
            LastName = lastName
        };

        var result = await userManager.CreateAsync(newUser, password);
        if (!result.Succeeded)
        {
            LogErrors($"Failed to create seed user '{userName}'", result.Errors);
            return;
        }

        var roleResult = await userManager.AddToRoleAsync(newUser, role);
        if (!roleResult.Succeeded)
        {
            LogErrors($"Failed to add '{userName}' to {role} role", roleResult.Errors);
            return;
        }

        logger.LogInformation("Created seed user {UserName} with role {Role}.", userName, role);
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
