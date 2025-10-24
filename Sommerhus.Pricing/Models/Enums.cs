using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Models;

public enum PriceScope { PerNight = 1, PerBooking = 2 }          // hvor anvendes beløbet
public enum AdjustmentKind { Absolute = 1, Percent = 2 }          // kr. eller %
public enum ModifierTrigger { Always = 1, Weekend = 2, MinNights = 3 } // hvornår gælder den