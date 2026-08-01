using AtasApi.Configuration;
using Xunit;

namespace AtasApi.Tests;

public class ConfigurationSettingsTests
{
    [Fact]
    public void AppSettings_DefaultRateLimits_ShouldBeMorePermissive()
    {
        var settings = new AppSettings();

        Assert.Equal(30, settings.RateLimit.LoginAttemptsPerMinute);
        Assert.Equal(20, settings.RateLimit.RegisterAttemptsPerMinute);
        Assert.Equal(1000, settings.RateLimit.DefaultPerHour);
    }
}
