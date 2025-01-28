using Microsoft.AspNetCore.Mvc;
using CurrencyRates.Services;
using CurrencyRates.Models;

namespace CurrencyRates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyController : ControllerBase
    {
        private readonly INbpService _nbpService;
        private readonly ILogger<CurrencyController> _logger;

        public CurrencyController(INbpService nbpService, ILogger<CurrencyController> logger)
        {
            _nbpService = nbpService;
            _logger = logger;
        }

        // Endpoint do pobierania listy dostępnych walut
        [HttpGet("available")]
        public async Task<ActionResult<List<Currency>>> GetAvailableCurrencies()
        {
            try
            {
                var currencies = await _nbpService.GetAvailableCurrencies();
                return Ok(currencies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania listy walut");
                return StatusCode(500, "Wystąpił błąd podczas pobierania listy walut");
            }
        }

        // Endpoint do sprawdzania kursu względem PLN
        [HttpGet("rate/{currencyCode}")]
        public async Task<ActionResult<object>> GetRateAgainstPLN(string currencyCode)
        {
            try
            {
                var rate = await _nbpService.GetCurrentRate(currencyCode.Trim().ToUpper());
                if (rate == null)
                    return NotFound($"Nie znaleziono kursu dla waluty {currencyCode}");

                return Ok(new
                {
                    Currency = rate.CurrencyCode,
                    RateAgainstPLN = rate.Rate,
                    Date = rate.Date,
                    Description = $"1 {rate.CurrencyCode} = {rate.Rate:F4} PLN"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas pobierania kursu dla {currencyCode}");
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych");
            }
        }

        // Endpoint do porównywania dwóch dowolnych walut
        [HttpGet("compare/{baseCurrency}/{targetCurrency}")]
        public async Task<ActionResult<object>> CompareCurrencies(string baseCurrency, string targetCurrency)
        {
            try
            {
                var rate = await _nbpService.GetCurrencyComparison(
                    baseCurrency.Trim().ToUpper(),
                    targetCurrency.Trim().ToUpper()
                );

                return Ok(new
                {
                    BaseCurrency = baseCurrency.ToUpper(),
                    TargetCurrency = targetCurrency.ToUpper(),
                    Rate = Math.Round(rate, 4),
                    Description = $"1 {baseCurrency.ToUpper()} = {Math.Round(rate, 4)} {targetCurrency.ToUpper()}",
                    Date = DateTime.UtcNow.Date
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas porównywania walut");
                return StatusCode(500, "Wystąpił błąd podczas porównywania walut");
            }
        }

        // Historia kursu względem PLN
        [HttpGet("history/{currencyCode}")]
        public async Task<ActionResult<object>> GetRateHistory(
        string currencyCode,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
        {
            try
            {
                var rates = await _nbpService.GetRatesByDateRange(
                    currencyCode.Trim().ToUpper(),
                    startDate,
                    endDate
                );

                return Ok(new
                {
                    Currency = currencyCode.ToUpper(),
                    BaseCurrency = "PLN",
                    StartDate = startDate,
                    EndDate = endDate,
                    Rates = rates.Select(r => new
                    {
                        Date = r.Date,
                        Rate = r.Rate,
                        Description = $"1 {r.CurrencyCode} = {r.Rate:F4} PLN"
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas pobierania historii kursów dla {currencyCode}");
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych historycznych");
            }
        }

        // Historia kursu między dwiema walutami:
        [HttpGet("history/compare/{baseCurrency}/{targetCurrency}")]
        public async Task<ActionResult<object>> GetComparisonHistory(
        string baseCurrency,
        string targetCurrency,
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
        {
            try
            {
                // Pobieramy historię obu walut
                var baseRates = await _nbpService.GetRatesByDateRange(baseCurrency.ToUpper(), startDate, endDate);
                var targetRates = await _nbpService.GetRatesByDateRange(targetCurrency.ToUpper(), startDate, endDate);

                // Łączymy i przeliczamy kursy
                var comparisonRates = baseRates
                    .Join(targetRates,
                        b => b.Date.Date,
                        t => t.Date.Date,
                        (b, t) => new
                        {
                            Date = b.Date,
                            Rate = b.Rate / t.Rate,
                            Description = $"1 {baseCurrency} = {(b.Rate / t.Rate):F4} {targetCurrency}"
                        })
                    .OrderBy(r => r.Date)
                    .ToList();

                return Ok(new
                {
                    BaseCurrency = baseCurrency.ToUpper(),
                    TargetCurrency = targetCurrency.ToUpper(),
                    StartDate = startDate,
                    EndDate = endDate,
                    Rates = comparisonRates
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas pobierania historii porównania {baseCurrency} do {targetCurrency}");
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych historycznych");
            }
        }
    }

}