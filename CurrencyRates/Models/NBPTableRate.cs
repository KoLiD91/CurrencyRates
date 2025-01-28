namespace CurrencyRates.Models
{
    public class NBPTableRate
    {
        public string Currency { get; set; }     // Nazwa waluty po polsku
        public string Code { get; set; }         // Kod waluty (np. USD, EUR)
        public decimal Mid { get; set; }         // Kurs średni
    }
}
