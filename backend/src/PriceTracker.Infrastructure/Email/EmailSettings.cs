namespace PriceTracker.Infrastructure.Email;

public class EmailSettings
{
    public const string SectionName = "Email";

    public bool Enabled { get; set; }
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@pricetracker.local";
    public string FromName { get; set; } = "Akıllı Fiyat Takip";
}
