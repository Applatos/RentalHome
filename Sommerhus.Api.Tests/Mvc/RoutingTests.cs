using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Sommerhus.Api.Tests.Mvc;

/// <summary>
/// HousesController and AreasController exist both publicly and in admin. Links must reach the
/// one the caller means: the admin controllers carry the "Admin" area, public ones none.
/// </summary>
public sealed class RoutingTests(MvcAppFixture app) : IClassFixture<MvcAppFixture>
{
    private static readonly Guid Id = Guid.Parse("11111111-2222-3333-4444-555555555555");

    [Theory]
    [InlineData("Index", "Houses", null, "/")]
    [InlineData("Houses", "Houses", null, "/houses")]
    [InlineData("Details", "Houses", null, "/houses/11111111-2222-3333-4444-555555555555")]
    [InlineData("Index", "Areas", null, "/areas")]
    [InlineData("Details", "Areas", null, "/areas/11111111-2222-3333-4444-555555555555")]
    [InlineData("Index", "Houses", "Admin", "/admin/houses")]
    [InlineData("Details", "Houses", "Admin", "/admin/houses/11111111-2222-3333-4444-555555555555")]
    [InlineData("Index", "Areas", "Admin", "/admin/areas")]
    [InlineData("Details", "Areas", "Admin", "/admin/areas/11111111-2222-3333-4444-555555555555")]
    public void ExplicitArea_PicksPublicOrAdminController(string action, string controller, string? area, string expected)
    {
        var links = app.Mvc.Services.GetRequiredService<LinkGenerator>();
        var values = new RouteValueDictionary();
        if (area is not null)
            values["area"] = area;
        if (action == "Details")
            values["id"] = Id;

        var path = links.GetPathByAction(action, controller, values);

        path.Should().Be(expected);
    }

    [Theory]
    // A public page (the area page's "view house" link) reaches the public house page.
    [InlineData(null, "Areas", "Details", "Details", "Houses", "/houses/11111111-2222-3333-4444-555555555555")]
    [InlineData(null, "Areas", "Details", "Index", "Areas", "/areas")]
    // An admin page stays in admin, also when it names another controller.
    [InlineData("Admin", "Features", "Index", "Index", "Houses", "/admin/houses")]
    [InlineData("Admin", "Houses", "Details", "Details", "Houses", "/admin/houses/11111111-2222-3333-4444-555555555555")]
    [InlineData("Admin", "HouseGroups", "Details", "Index", "Areas", "/admin/areas")]
    // Admin pages still reach the controllers that only exist publicly (the layout's account links).
    [InlineData("Admin", "Houses", "Index", "Logout", "Account", "/account/logout")]
    [InlineData("Admin", "Houses", "Index", "Houses", "Houses", "/houses")]
    public void AmbientArea_KeepsLinksOnTheSameSide(
        string? currentArea, string currentController, string currentAction,
        string action, string controller, string expected)
    {
        var links = app.Mvc.Services.GetRequiredService<LinkGenerator>();
        var current = new DefaultHttpContext { RequestServices = app.Mvc.Services };
        current.Request.RouteValues = new RouteValueDictionary
        {
            ["controller"] = currentController,
            ["action"] = currentAction
        };
        if (currentArea is not null)
            current.Request.RouteValues["area"] = currentArea;

        var values = action == "Details" ? new RouteValueDictionary { ["id"] = Id } : null;
        var path = links.GetPathByAction(current, action, controller, values);

        path.Should().Be(expected);
    }
}
