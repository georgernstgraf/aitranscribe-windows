using AITranscribe.Console;
using AITranscribe.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AITranscribe.Integration.Tests;

public class CompositionRootTestHelper : IDisposable
{
    public IServiceProvider ServiceProvider { get; }
    public string TempDbPath { get; }
    public ConfigManager ConfigManager { get; }
    public AppConfig Config { get; }

    public CompositionRootTestHelper()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ait_int_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        TempDbPath = Path.Combine(tempDir, "prompts.sqlite");

        ConfigManager = ConfigManager.CreateDefault();
        Config = ConfigManager.Load();

        ServiceProvider = CompositionRoot.Build(ConfigManager, Config, TempDbPath);
    }

    public void Dispose()
    {
        try
        {
            var dir = Path.GetDirectoryName(TempDbPath);
            if (dir is not null && Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
        catch { }
    }
}