using System.Net.Http.Json;
using CurrencyRates.Models;
using CurrencyRates.Data;
using Microsoft.EntityFrameworkCore;

namespace CurrencyRates.Services
{
    public class NbpService : INbpService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NbpService> _logger;
        private const string NBP_API_BASE_URL = "http://api.nbp.pl/api/exchangerates/rates/a/";

        public NbpService(
            HttpClient httpClient,
            ApplicationDbContext context,
            ILogger<NbpService> logger)
        {
            _httpClient = httpClient;
            _context = context;
            _logger = logger;

            // Konfiguracja klienta HTTP
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task<ExchangeRate> GetCurrentRate(string currencyCode)
        {
            try
            {
                _logger.LogInformation($"Rozpoczynam pobieranie kursu dla waluty {currencyCode}");

                // Najpierw próbujemy pobrać dzisiejszy kurs
                var todayUrl = $"https://api.nbp.pl/api/exchangerates/rates/a/{currencyCode}/today?format=json";
                _logger.LogInformation($"Próba pobrania dzisiejszego kursu: {todayUrl}");

                try
                {
                    var response = await _httpClient.GetFromJsonAsync<NBPResponse>(todayUrl);
                    if (response?.Rates?.Any() == true)
                    {
                        var rate = response.Rates.First();
                        _logger.LogInformation($"Pobrano dzisiejszy kurs dla {currencyCode}: {rate.Mid}");
                        return CreateExchangeRate(currencyCode, rate, response.Table);
                    }
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogInformation($"Brak dzisiejszego kursu dla {currencyCode}, pobieram ostatni dostępny");

                    // Jeśli nie ma dzisiejszego kursu, pobieramy ostatni dostępny
                    var lastUrl = $"https://api.nbp.pl/api/exchangerates/rates/a/{currencyCode}/last/1?format=json";
                    var lastResponse = await _httpClient.GetFromJsonAsync<NBPResponse>(lastUrl);

                    if (lastResponse?.Rates?.Any() == true)
                    {
                        var rate = lastResponse.Rates.First();
                        _logger.LogInformation($"Pobrano ostatni dostępny kurs dla {currencyCode}: {rate.Mid}");
                        return CreateExchangeRate(currencyCode, rate, lastResponse.Table);
                    }
                }

                _logger.LogWarning($"Nie znaleziono żadnego kursu dla waluty {currencyCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Wystąpił nieoczekiwany błąd podczas pobierania kursu dla {currencyCode}");
                throw;
            }
        }

        // Pomocnicza metoda do tworzenia obiektu ExchangeRate
        private ExchangeRate CreateExchangeRate(string currencyCode, NBPRate rate, string table)
        {
            return new ExchangeRate
            {
                CurrencyCode = currencyCode,
                Rate = rate.Mid,
                Date = DateTime.SpecifyKind(rate.EffectiveDate, DateTimeKind.Utc),
                TableType = table,
                FetchDate = DateTime.UtcNow
            };
        }

        private async Task SaveRateToDatabase(ExchangeRate rate)
        {
            // Sprawdzamy czy już nie mamy tego kursu
            var existingRate = await _context.ExchangeRates
                .FirstOrDefaultAsync(r =>
                    r.CurrencyCode == rate.CurrencyCode &&
                    r.Date.Date == rate.Date.Date);

            if (existingRate == null)
            {
                _context.ExchangeRates.Add(rate);
                await _context.SaveChangesAsync();
                _logger.LogInformation($"Zapisano nowy kurs dla {rate.CurrencyCode} z dnia {rate.Date:yyyy-MM-dd}");
            }
        }
        public async Task<IEnumerable<ExchangeRate>> GetRatesByDateRange(string currencyCode, DateTime startDate, DateTime endDate)
        {
            try
            {
                // Format daty wymagany przez API NBP to YYYY-MM-DD
                var formattedStartDate = startDate.ToString("yyyy-MM-dd");
                var formattedEndDate = endDate.ToString("yyyy-MM-dd");

                // URL do API NBP
                var url = $"{NBP_API_BASE_URL}{currencyCode}/{formattedStartDate}/{formattedEndDate}/?format=json";

                // Pobieramy dane z API
                var response = await _httpClient.GetFromJsonAsync<NBPResponse>(url);

                if (response?.Rates == null || !response.Rates.Any())
                    return Enumerable.Empty<ExchangeRate>();

                var rates = response.Rates.Select(rate => new ExchangeRate
                {
                    CurrencyCode = currencyCode,
                    Rate = rate.Mid,
                    Date = rate.EffectiveDate,
                    TableType = response.Table,
                    FetchDate = DateTime.UtcNow
                }).ToList();

                foreach (var rate in rates)
                {
                    await SaveRateToDatabase(rate);
                }

                return rates;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas pobierania kursów dla {currencyCode} w zakresie dat {startDate:yyyy-MM-dd} - {endDate:yyyy-MM-dd}");
                throw;
            }
        }

        public async Task<List<Currency>> GetAvailableCurrencies()
        {
            try
            {
                // Pobieramy tabelę A z aktualnymi kursami
                var url = "http://api.nbp.pl/api/exchangerates/tables/a/?format=json";
                var response = await _httpClient.GetFromJsonAsync<List<NBPTable>>(url);

                if (response == null || !response.Any())
                    return new List<Currency>();

                // Przekształcamy odpowiedź na listę walut
                var currencies = response[0].Rates.Select(r => new Currency
                {
                    Code = r.Code,
                    Name = r.Currency,
                    TableType = response[0].Table
                }).ToList();

                // Dodajemy PLN do listy
                currencies.Add(new Currency
                {
                    Code = "PLN",
                    Name = "złoty polski",
                    TableType = "A"
                });

                return currencies.OrderBy(c => c.Name).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy walut");
                throw;
            }
        }
        public async Task<decimal> GetCurrencyComparison(string baseCurrency, string targetCurrency)
        {
            try
            {
                // Obsługa specjalnych przypadków
                if (baseCurrency == targetCurrency)
                    return 1.0m;

                // Jeśli jedna z walut to PLN, wystarczy jeden kurs
                if (baseCurrency == "PLN")
                    return 1 / (await GetCurrentRate(targetCurrency)).Rate;

                if (targetCurrency == "PLN")
                    return (await GetCurrentRate(baseCurrency)).Rate;

                // Pobieramy kursy obu walut względem PLN
                var baseRate = await GetCurrentRate(baseCurrency);
                var targetRate = await GetCurrentRate(targetCurrency);

                if (baseRate == null || targetRate == null)
                    throw new Exception("Nie udało się pobrać kursów walut");

                // Kurs obcych walut
                return baseRate.Rate / targetRate.Rate;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas porównywania walut {baseCurrency} i {targetCurrency}");
                throw;
            }
        }
    }
}