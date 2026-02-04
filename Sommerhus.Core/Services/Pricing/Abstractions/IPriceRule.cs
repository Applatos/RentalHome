using System.Threading;
using System.Threading.Tasks;

namespace Sommerhus.Core.Services.Pricing.Abstractions;

public interface IPriceRule
{
    Task ApplyAsync(PricingContext ctx, CancellationToken ct);
}
