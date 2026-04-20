using System.Text.Json;
using AITranscribe.Core.Models;

namespace AITranscribe.Core.Configuration;

public class ConfigManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _configPath;
    private readonly string? _pythonConfigDir;

    public ConfigManager(string configPath, string? pythonConfigDir = null)
    {
        _configPath = configPath;
        _pythonConfigDir = pythonConfigDir;
    }

    public static ConfigManager CreateDefault()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configDir = Path.Combine(appData, "AITranscribe");
        var configPath = Path.Combine(configDir, "config.json");

        var pythonConfigDir = FindPythonConfigDir();

        return new ConfigManager(configPath, pythonConfigDir);
    }

    private static string? FindPythonConfigDir()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var candidates = new[]
        {
            Path.Combine(appData, "aitranscribe"),
            Path.Combine(appData, "AITranscribe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "aitranscribe"),
        };

        foreach (var dir in candidates)
        {
            if (File.Exists(Path.Combine(dir, "config")))
                return dir;
        }

        return Path.Combine(appData, "aitranscribe");
    }

    public AppConfig Load()
    {
        if (File.Exists(_configPath))
        {
            var json = File.ReadAllText(_configPath);
            return JsonSerializer.Deserialize<AppConfig>(json, JsonOptions)
                ?? AppConfig.CreateDefault();
        }

        var migrated = TryMigrateFromPython();
        if (migrated is not null)
        {
            Save(migrated);
            return migrated;
        }

        var defaults = AppConfig.CreateDefault();
        Save(defaults);
        return defaults;
    }

    public void Save(AppConfig config)
    {
        var dir = Path.GetDirectoryName(_configPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configPath, json);
    }

    public void EnsureExists()
    {
        if (!File.Exists(_configPath))
        {
            Save(AppConfig.CreateDefault());
        }
        else
        {
            var dir = Path.GetDirectoryName(_configPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }

    private AppConfig? TryMigrateFromPython()
    {
        if (_pythonConfigDir is null)
            return null;

        var dotenvPath = Path.Combine(_pythonConfigDir, "config");
        if (!File.Exists(dotenvPath))
            return null;

        var values = ParseDotenv(dotenvPath);
        if (values.Count == 0)
            return null;

        var defaults = AppConfig.CreateDefault();

        var groqApiKey = GetValue(values, "GROQ_API_KEY", defaults.Groq.ApiKey);
        var groqSttModel = GetValue(values, "GROQ_STT_MODEL", defaults.Groq.SttModel);
        var llmProvider = GetValue(values, "LLM_PROVIDER", defaults.Llm.Provider).ToLowerInvariant();
        var preProcessMode = NormalizePreProcessMode(GetValue(values, "PRE_PROCESS_MODE", "english"));
        var inputSource = NormalizeInputSource(GetValue(values, "TRANSCRIBE_SOURCE", "microphone"));
        var lastFilePath = GetValue(values, "LAST_FILE_PATH", "");
        var verbose = ParseBool(GetValue(values, "VERBOSE_ERRORS", "false"));

        var openRouterApiKey = GetValue(values, "OPENROUTER_API_KEY", defaults.OpenRouter.ApiKey);
        var openRouterModel = GetValue(values, "OPENROUTER_LLM_MODEL", defaults.OpenRouter.Model);
        var cohereApiKey = GetValue(values, "COHERE_API_KEY", defaults.Cohere.ApiKey);
        var cohereModel = GetValue(values, "COHERE_LLM_MODEL", defaults.Cohere.Model);
        var zaiApiKey = GetValue(values, "ZAI_API_KEY", defaults.ZAi.ApiKey);
        var zaiModel = GetValue(values, "ZAI_LLM_MODEL", defaults.ZAi.Model);

        var llmModel = llmProvider switch
        {
            "openrouter" => openRouterModel,
            "cohere" => cohereModel,
            "z.ai" => zaiModel,
            _ => defaults.Llm.Model,
        };

        return new AppConfig(
            new GroqConfig(groqApiKey, groqSttModel),
            new LlmConfig(llmProvider, llmModel),
            preProcessMode,
            inputSource,
            lastFilePath,
            verbose,
            new OpenRouterConfig(openRouterApiKey, openRouterModel),
            new CohereConfig(cohereApiKey, cohereModel),
            new ZAiConfig(zaiApiKey, zaiModel)
        );
    }

    private static Dictionary<string, string> ParseDotenv(string path)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                continue;

            var eqIndex = trimmed.IndexOf('=');
            if (eqIndex < 0)
                continue;

            var key = trimmed[..eqIndex].Trim();
            var value = trimmed[(eqIndex + 1)..].Trim();

            if (value.StartsWith('"') && value.EndsWith('"') && value.Length >= 2)
                value = value[1..^1];
            else if (value.StartsWith('\'') && value.EndsWith('\'') && value.Length >= 2)
                value = value[1..^1];

            result[key] = value;
        }
        return result;
    }

    private static string GetValue(Dictionary<string, string> dict, string key, string defaultValue)
    {
        return dict.TryGetValue(key, out var val) ? val : defaultValue;
    }

    private static PreProcessMode NormalizePreProcessMode(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "raw" => PreProcessMode.Raw,
            "cleanup" => PreProcessMode.Cleanup,
            "english" => PreProcessMode.English,
            _ => PreProcessMode.English,
        };
    }

    private static string NormalizeInputSource(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();
        return normalized is "microphone" or "file" ? normalized : "microphone";
    }

    private static bool ParseBool(string value)
    {
        return value.Trim().ToLowerInvariant() is "1" or "true" or "yes" or "on";
    }
}
