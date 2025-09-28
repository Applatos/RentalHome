using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Contracts.Dtos.Shared;

// ---------- Shared DTO helpers ----------
public sealed record LookupItem(Guid Id, string Label);