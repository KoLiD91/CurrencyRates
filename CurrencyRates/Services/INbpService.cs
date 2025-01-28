using CurrencyRates.Models;

namespace CurrencyRates.Services
{
    public interface INbpService
    {
        Task<ExchangeRate> GetCurrentRate(string currencyCode);
        Task<IEnumerable<ExchangeRate>> GetRatesByDateRange(string currencyCode, DateTime startDate, DateTime endDate);
        Task<List<Currency>> GetAvailableCurrencies();
        Task<decimal> GetCurrencyComparison(string baseCurrency, string targetCurrency);
    }
}