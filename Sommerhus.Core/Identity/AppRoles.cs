namespace Sommerhus.Core.Identity;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string HouseOwner = "HouseOwner";
    public const string User = "User";

    public static readonly string[] All = [Admin, HouseOwner, User];
}
