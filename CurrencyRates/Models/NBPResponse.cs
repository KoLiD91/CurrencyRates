using System;
using System.Collections.Generic;

namespace CurrencyRates.Models
{
    public class NBPResponse
    {
        public string Table { get; set; }
        public string Currency { get; set; }
        public string Code { get; set; }
        public List<NBPRate> Rates { get; set; }
    }
}