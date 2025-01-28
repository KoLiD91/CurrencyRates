using System;
using System.Collections.Generic;

namespace CurrencyRates.Models
{
    public class NBPRate
    {
        public string No { get; set; }
        public DateTime EffectiveDate { get; set; }
        public decimal Mid { get; set; }
    }
}