using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Contracts.Dtos.Admin.Features;

public record FeatureListItem(Guid Id, string Name, string Key, string ValueType, string? Unit, string? IconUrl);