using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Sommerhus.Pricing.Models
{
    public class SeasonSpan
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }

        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }

        public string Code { get; set; } = "A";

    }
}
