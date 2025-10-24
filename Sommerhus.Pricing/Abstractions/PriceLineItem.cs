using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Abstractions;

public record PriceLineItem(string Code, string Text, decimal Amount);