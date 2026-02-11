namespace Sommerhus.Core.Dtos.Security;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string HouseOwner = "HouseOwner";
    public const string User = "User";

    public static readonly string[] All = [Admin, HouseOwner, User];
}

/// <summary>Backward-compatible alias for <see cref="AppRoles"/>.</summary>
public static class AdminRoles
{
    public const string Admin = AppRoles.Admin;
}
