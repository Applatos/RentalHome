using System;

namespace Sommerhus.Domain.Models.Pricing;

public class SeasonSpan
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid CalendarId { get; set; }
    public SeasonCalendar Calendar { get; set; } = null!;

    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }

    public string Code { get; set; } = "A";
}
