using System.Threading;
using System.Threading.Tasks;

namespace Sommerhus.Application.Pricing.Abstractions;

public interface IPriceRule
{
    Task ApplyAsync(PricingContext ctx, CancellationToken ct);
}
