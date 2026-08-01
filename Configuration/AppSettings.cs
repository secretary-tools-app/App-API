namespace AtasApi.Configuration;

public class AppSettings
{
    public JwtSettings Jwt { get; set; } = new();
    public AdminSettings Admin { get; set; } = new();
    public string ConnectionString { get; set; } = "Data Source=database/atas.db";
    public string SchemaPath { get; set; } = "database/schema_inicial.sql";
    public RateLimitSettings RateLimit { get; set; } = new();
    public string[] AllowedOrigins { get; set; } = [];
    public string[] AllowedHosts { get; set; } = ["localhost", "127.0.0.1", "*.up.railway.app"];
}

public class AdminSettings
{
    /// <summary>Segredo para endpoints administrativos (header X-Admin-Secret). Defina via Admin__Secret.</summary>
    public string Secret { get; set; } = string.Empty;
}

public class JwtSettings
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "AtasApi";
    public string Audience { get; set; } = "AtasApp";
}

public class RateLimitSettings
{
    public int LoginAttemptsPerMinute { get; set; } = 5;
    public int RegisterAttemptsPerMinute { get; set; } = 10;
    public int DefaultPerHour { get; set; } = 50;
}