using PriceTracker.Application.DTOs;

namespace PriceTracker.Application.Interfaces;

public interface IPriceScraper
{
    /// <summary>
    /// Varyant odaklı ürün URL'ini headless Chrome ile ziyaret eder;
    /// fiyat, başlık, görsel ve stok bilgisini çıkarır.
    /// </summary>
    Task<ScrapeResultDto> ScrapeAsync(string productUrl, CancellationToken cancellationToken = default);
}
