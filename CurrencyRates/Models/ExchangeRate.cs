using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CurrencyRates.Models
{
    public class ExchangeRate
    {
        public int Id { get; set; }
        public string CurrencyCode { get; set; }
        public decimal Rate { get; set; }
        private DateTime date;
        public DateTime Date
        {
            get => date;
            set => date = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
        public string TableType { get; set; }
        private DateTime fetchDate;
        public DateTime FetchDate
        {
            get => fetchDate;
            set => fetchDate = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }
    }
}
