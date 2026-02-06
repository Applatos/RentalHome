using System;

namespace Sommerhus.Domain.Models.Pricing;

public class SeasonSpan
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string Code { get; set; } = "A";
}
