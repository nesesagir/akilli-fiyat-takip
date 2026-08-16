using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using PriceTracker.Application.Interfaces;

namespace PriceTracker.Infrastructure.Email;

public class MailKitEmailNotifier : IEmailNotifier
{
    private readonly EmailSettings _settings;
    private readonly ILogger<MailKitEmailNotifier> _logger;

    public MailKitEmailNotifier(IOptions<EmailSettings> settings, ILogger<MailKitEmailNotifier> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendDealAlertAsync(
        string toEmail,
        string productTitle,
        string productUrl,
        decimal currentPrice,
        decimal targetPrice,
        string currency,
        CancellationToken cancellationToken = default)
    {
        var subject = $"Fırsat Yakalandı! — {productTitle}";
        var html = BuildHtml(productTitle, productUrl, currentPrice, targetPrice, currency);

        if (!_settings.Enabled)
        {
            _logger.LogWarning(
                "E-posta KAPALI (Email:Enabled=false). Gerçek mail gitmedi. To={To}",
                toEmail);
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.Username) ||
            string.IsNullOrWhiteSpace(_settings.Password))
        {
            _logger.LogWarning(
                "E-posta açılmış ama Username/Password boş. .env dosyasını doldur. To={To}",
                toEmail);
            return false;
        }

        var from = string.IsNullOrWhiteSpace(_settings.FromAddress)
            ? _settings.Username
            : _settings.FromAddress;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = html };

        using var client = new SmtpClient();
        try
        {
            await client.ConnectAsync(
                _settings.Host,
                _settings.Port,
                _settings.UseSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                cancellationToken);

            await client.AuthenticateAsync(_settings.Username, _settings.Password, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            _logger.LogInformation("Fırsat e-postası GÖNDERİLDİ. To={To} Product={Product}", toEmail, productTitle);
            return true;
        }
        finally
        {
            if (client.IsConnected)
                await client.DisconnectAsync(true, cancellationToken);
        }
    }

    private static string BuildHtml(
        string title,
        string url,
        decimal currentPrice,
        decimal targetPrice,
        string currency)
    {
        return $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head><meta charset="utf-8" /></head>
            <body style="margin:0;padding:0;background:#f4f1ea;font-family:Segoe UI,Arial,sans-serif;">
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#f4f1ea;padding:32px 16px;">
                <tr>
                  <td align="center">
                    <table role="presentation" width="560" cellspacing="0" cellpadding="0" style="background:#1a2e22;border-radius:16px;overflow:hidden;">
                      <tr>
                        <td style="padding:28px 32px 8px;color:#c8e6c9;font-size:13px;letter-spacing:0.12em;text-transform:uppercase;">
                          Akıllı Fiyat Takip
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:8px 32px 0;color:#ffffff;font-size:28px;font-weight:700;line-height:1.25;">
                          Fırsat Yakalandı!
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:16px 32px 24px;color:#d7e5db;font-size:15px;line-height:1.5;">
                          Takip ettiğiniz ürün hedef fiyatın altına düştü.
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:0 24px 28px;">
                          <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background:#243f31;border-radius:12px;">
                            <tr>
                              <td style="padding:20px 24px;">
                                <div style="color:#ffffff;font-size:17px;font-weight:600;margin-bottom:12px;">{System.Net.WebUtility.HtmlEncode(title)}</div>
                                <div style="color:#a5d6a7;font-size:14px;">Güncel fiyat</div>
                                <div style="color:#ffffff;font-size:26px;font-weight:700;margin:4px 0 12px;">
                                  {currentPrice:N2} {System.Net.WebUtility.HtmlEncode(currency)}
                                </div>
                                <div style="color:#b0bec5;font-size:13px;">
                                  Hedef: {targetPrice:N2} {System.Net.WebUtility.HtmlEncode(currency)}
                                </div>
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                      <tr>
                        <td style="padding:0 32px 32px;" align="center">
                          <a href="{System.Net.WebUtility.HtmlEncode(url)}"
                             style="display:inline-block;background:#66bb6a;color:#0d1f14;text-decoration:none;font-weight:700;font-size:14px;padding:14px 28px;border-radius:999px;">
                            Ürüne Git
                          </a>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
              </table>
            </body>
            </html>
            """;
    }
}
