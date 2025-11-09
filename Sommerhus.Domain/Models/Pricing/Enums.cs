namespace Sommerhus.Domain.Models.Pricing;

public enum PriceScope
{
    PerNight = 1,
    PerBooking = 2,
}

public enum AdjustmentKind
{
    Absolute = 1,
    Percent = 2,
}

public enum ModifierTrigger
{
    Always = 1,
    Weekend = 2,
    MinNights = 3,
}
