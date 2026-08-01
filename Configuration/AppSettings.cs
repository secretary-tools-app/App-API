namespace AtasApi.Configuration;

public class AppSettings
{
    public JwtSettings Jwt { get; set; } = new();
    public string ConnectionString { get; set; } = "Data Source=database/atas.db";
    public string SchemaPath { get; set; } = "database/schema_inicial.sql";
    public RateLimitSettings RateLimit { get; set; } = new();
    public string[] AllowedOrigins { get; set; } = [];
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