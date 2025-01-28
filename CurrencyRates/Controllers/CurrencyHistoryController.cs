using Microsoft.AspNetCore.Mvc;
using CurrencyRates.Services;
using CurrencyRates.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CurrencyRates.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CurrencyHistoryController : ControllerBase
    {
        private readonly INbpService _nbpService;
        private readonly ILogger<CurrencyHistoryController> _logger;

        public CurrencyHistoryController(INbpService nbpService, ILogger<CurrencyHistoryController> logger)
        {
            _nbpService = nbpService;
            _logger = logger;
        }

        //Pełną historię kursów dla wybranego roku
        [HttpGet("history/{currencyCode}/yearly/{year}")]
        public async Task<ActionResult<object>> GetYearlyRates(string currencyCode, int year)
        {
            try
            {
                var startDate = new DateTime(year, 1, 1);
                var endDate = new DateTime(year, 12, 31);
                var rates = await _nbpService.GetRatesByDateRange(currencyCode.Trim().ToUpper(), startDate, endDate);

                return Ok(new
                {
                    Currency = currencyCode.ToUpper(),
                    Year = year,
                    Rates = rates,
                    AverageRate = rates.Any() ? rates.Average(r => r.Rate) : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas pobierania kursów rocznych dla {currencyCode}");
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych");
            }
        }

        //Hisotria kwartałów
        [HttpGet("history/{currencyCode}/quarterly/{year}/{quarter}")]
        public async Task<ActionResult<object>> GetQuarterlyRates(string currencyCode, int year, int quarter)
        {
            try
            {
                var startDate = new DateTime(year, (quarter - 1) * 3 + 1, 1);
                var endDate = startDate.AddMonths(3).AddDays(-1);
                var rates = await _nbpService.GetRatesByDateRange(currencyCode.Trim().ToUpper(), startDate, endDate);

                return Ok(new
                {
                    Currency = currencyCode.ToUpper(),
                    Year = year,
                    Quarter = quarter,
                    Rates = rates,
                    AverageRate = rates.Any() ? rates.Average(r => r.Rate) : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas pobierania kursów kwartalnych dla {currencyCode}");
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych");
            }
        }

        //Historia miesięczna
        [HttpGet("history/{currencyCode}/monthly/{year}/{month}")]
        public async Task<ActionResult<object>> GetMonthlyRates(string currencyCode, int year, int month)
        {
            try
            {
                var startDate = new DateTime(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);
                var rates = await _nbpService.GetRatesByDateRange(currencyCode.Trim().ToUpper(), startDate, endDate);

                return Ok(new
                {
                    Currency = currencyCode.ToUpper(),
                    Year = year,
                    Month = month,
                    Rates = rates,
                    AverageRate = rates.Any() ? rates.Average(r => r.Rate) : 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas pobierania kursów miesięcznych dla {currencyCode}");
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych");
            }
        }

        //Kurs z danego dnia
        [HttpGet("history/{currencyCode}/daily/{date}")]
        public async Task<ActionResult<object>> GetDailyRate(string currencyCode, DateTime date)
        {
            try
            {
                var rates = await _nbpService.GetRatesByDateRange(currencyCode.Trim().ToUpper(), date, date);
                var rate = rates.FirstOrDefault();

                if (rate == null)
                    return NotFound($"Nie znaleziono kursu dla waluty {currencyCode} w dniu {date:yyyy-MM-dd}");

                return Ok(new
                {
                    Currency = currencyCode.ToUpper(),
                    Date = date,
                    Rate = rate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Błąd podczas pobierania kursu dziennego dla {currencyCode}");
                return StatusCode(500, "Wystąpił błąd podczas pobierania danych");
            }
        }
    }
}