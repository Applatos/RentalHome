using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Models;

public class SeasonCode
{
    [Key]
    public string Code { get; set; } = "A";
    public string Name { get; set; } = "Højsæson";
    public string Color { get; set; }
    public int SortOrder { get; set; }
}