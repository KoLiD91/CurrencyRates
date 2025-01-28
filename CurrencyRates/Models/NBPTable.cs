namespace CurrencyRates.Models
{
    public class NBPTable
    {
        public string Table { get; set; }        // Typ tabeli (A, B lub C)
        public string No { get; set; }           // Numer tabeli
        public DateTime EffectiveDate { get; set; }
        public List<NBPTableRate> Rates { get; set; }
    }
}
