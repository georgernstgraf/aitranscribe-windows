using AITranscribe.Core.Configuration;

namespace AITranscribe.Integration.Tests;

public static class LiveTestConfig
{
    public static bool IsLiveTest =>
        Environment.GetEnvironmentVariable("LIVE_TEST") == "1";

    public static ConfigManager CreateConfigManager()
    {
        return ConfigManager.CreateDefault();
    }

    public static AppConfig LoadConfig()
    {
        var configManager = CreateConfigManager();
        return configManager.Load();
    }

    public static bool HasRequiredApiKeys(AppConfig config)
    {
        return !string.IsNullOrEmpty(config.Groq.ApiKey);
    }

    public static void SkipIfNotLive()
    {
        if (!IsLiveTest)
            throw new SkipException(SkipReason);
    }

    public const string SkipReason = "Set LIVE_TEST=1 env var to run integration tests";
}