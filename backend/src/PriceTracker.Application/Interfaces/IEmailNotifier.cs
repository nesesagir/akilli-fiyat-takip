namespace PriceTracker.Application.Interfaces;

public interface IEmailNotifier
{
    /// <returns>true = gerçek SMTP ile gönderildi; false = kapalı/eksik ayar (tekrar denenecek).</returns>
    Task<bool> SendDealAlertAsync(
        string toEmail,
        string productTitle,
        string productUrl,
        decimal currentPrice,
        decimal targetPrice,
        string currency,
        CancellationToken cancellationToken = default);
}
