using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Sommerhus.Mvc.Models.Admin;

public sealed class AreaFormViewModel
{
    public Guid? Id { get; init; }
    public string? Slug { get; init; }
    public string Title { get; init; } = string.Empty;
    public string SubmitText { get; init; } = "Gem";
    public AreaFormInput Input { get; init; } = new();
    public IReadOnlyList<SelectListItem> Cities { get; init; } = Array.Empty<SelectListItem>();
    public bool IsEdit => Id.HasValue;
}
